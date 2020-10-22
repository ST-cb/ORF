﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace ORF.Entities
{
    public class CostItem : IfcRootWrapper<IIfcCostItem>, IEntity
    {
        public CostItem(IIfcCostItem item) : base(item)
        {
            var rels = item.IsNestedBy.ToList();
            _rel = rels.FirstOrDefault();

            var children = rels.SelectMany(r => r.RelatedObjects)
                .OfType<IIfcCostItem>()
                .Select(i => new CostItem(i));

            _children = new HashSet<CostItem>(children);
            Quantities = new QuantityCollection(this);
        }

        public string Identifier { get => Entity.Identification; set => Entity.Identification = value; }

        private IIfcRelNests _rel;
        private readonly HashSet<CostItem> _children = new HashSet<CostItem>();
        public IEnumerable<CostItem> Children => _children.ToList().AsReadOnly();

        public void AddChild(CostItem child)
        {
            if (!_children.Add(child))
                return;
            var rel = _rel ?? (_rel = Create.RelNests(r => r.RelatingObject = Entity));
            rel.RelatedObjects.Add(child.Entity);
        }

        public void RemoveChild(CostItem child)
        {
            if (!_children.Remove(child))
                return;

            Entity.IsNestedBy.ToList()
                .ForEach(r => r.RelatedObjects.Remove(child.Entity));
        }

        public bool HasChild(CostItem child) => _children.Contains(child);

        public QuantityCollection Quantities { get; }

        public double TotalQuantity => Quantities.Sum(q => q.Value);

    }

    public class QuantityCollection : ICollection<Quantity>
    {
        private readonly HashSet<Quantity> _quantities = new HashSet<Quantity>();
        private readonly IList<IIfcPhysicalQuantity> _native;
        private readonly Create create;


        internal QuantityCollection(CostItem item)
        {
            _quantities = new HashSet<Quantity>(item.Entity.CostQuantities
                .OfType<IIfcPhysicalSimpleQuantity>()
                .Select(q => new Quantity(q)));
            _native = item.Entity.CostQuantities;
            create = new Create(item.Entity.Model);
        }

        public int Count => _quantities.Count;

        public bool IsReadOnly => false;


        public void Add(Quantity item)
        {
            if (_quantities.Any() && _quantities.First().Type != item.Type)
                throw new ArgumentException("All quantities should be of the same type", nameof(item));

            if (_quantities.Add(item))
                _native.Add(item.Entity);
        }

        public Quantity AddArea()
        {
            var value = new Quantity(create.QuantityArea());
            Add(value);
            return value;
        }

        public Quantity AddCount()
        {
            var value = new Quantity(create.QuantityCount());
            Add(value);
            return value;
        }

        public Quantity AddLength()
        {
            var value = new Quantity(create.QuantityLength());
            Add(value);
            return value;
        }

        public Quantity AddTime()
        {
            var value = new Quantity(create.QuantityTime());
            Add(value);
            return value;
        }

        public Quantity AddVolume()
        {
            var value = new Quantity(create.QuantityVolume());
            Add(value);
            return value;
        }

        public Quantity AddWeight()
        {
            var value = new Quantity(create.QuantityWeight());
            Add(value);
            return value;
        }

        public void Clear()
        {
            _quantities.Clear();
            _native.Clear();
        }

        public bool Contains(Quantity item)
        {
            return _quantities.Contains(item);
        }

        public void CopyTo(Quantity[] array, int arrayIndex)
        {
            _quantities.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Quantity> GetEnumerator()
        {
            return _quantities.GetEnumerator();
        }

        public bool Remove(Quantity item)
        {
            if (_quantities.Remove(item))
                return _native.Remove(item.Entity);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}