﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabularEditor.TOMWrapper.Utils
{
    /// <summary>
    /// A DependsOnList holds a dictionary of all objects that a specific opject depends on. Each entry contains
    /// a list of ObjectReferences specifying the details of how the object is referenced.
    /// </summary>
    public class DependsOnList : IReadOnlyDictionary<IDaxObject, List<ObjectReference>>
    {
        internal readonly IDaxDependantObject Parent;
        internal DependsOnList(IDaxDependantObject parent)
        {
            Parent = parent;
        }

        private Dictionary<IDaxObject, List<ObjectReference>> InternalDictionary = new Dictionary<IDaxObject, List<ObjectReference>>();
        private List<IDaxObject> InternalList = new List<IDaxObject>();

        internal void Add(IDaxObject dependsOn, DAXProperty property, int fromChar, int toChar, bool fullyQualified)
        {
            var dep = new ObjectReference { property = property, from = fromChar, to = toChar, fullyQualified = fullyQualified };
            List<ObjectReference> depList;
            if (!InternalDictionary.TryGetValue(dependsOn, out depList))
            {
                depList = new List<ObjectReference>();
                InternalDictionary.Add(dependsOn, depList);
                InternalList.Add(dependsOn);
            }
            depList.Add(dep);
        }

        internal void UpdateRef(IDaxObject renamedObj)
        {
            List<ObjectReference> depList;
            if (TryGetValue(renamedObj, out depList))
            {
                var pos = new int[5];
                var sbs = new StringBuilder[5];
                for (var i = 0; i < 5; i++) sbs[i] = new StringBuilder();

                // Loop through all dependencies:
                foreach (var dep in depList)
                {
                    var propIx = (int)dep.property;

                    var sb = sbs[propIx];

                    sb.Append(Parent.GetDAX(dep.property).Substring(pos[propIx], dep.from - pos[propIx]));
                    sb.Append(dep.fullyQualified ? renamedObj.DaxObjectFullName : renamedObj.DaxObjectName);
                    pos[propIx] = dep.to + 1;
                }

                // Finalize:
                for (var i = 0; i < 5; i++)
                {
                    if (pos[i] > 0)
                    {
                        sbs[i].Append(Parent.GetDAX((DAXProperty)i).Substring(pos[i]));
                        Parent.SetDAX((DAXProperty)i, sbs[i].ToString());
                    }
                }
            }
        }

        public IEnumerable<Measure> Measures { get { return InternalList.OfType<Measure>(); } }
        public IEnumerable<Column> Columns { get { return InternalList.OfType<Column>(); } }
        public IEnumerable<Table> Tables { get { return InternalList.OfType<Table>(); } }


        #region IDictionary members
        public void Clear()
        {
            InternalDictionary.Clear();
            InternalList.Clear();
        }
        public void Remove(IDaxObject key)
        {
            InternalDictionary.Remove(key);
            InternalList.Remove(key);
        }

        public List<ObjectReference> this[IDaxObject key]
        {
            get
            {
                return InternalDictionary[key];
            }
        }

        public int Count
        {
            get
            {
                return InternalDictionary.Count;
            }
        }

        public IEnumerable<IDaxObject> Keys
        {
            get
            {
                return InternalList;
            }
        }

        public IEnumerable<List<ObjectReference>> Values
        {
            get
            {
                return InternalDictionary.Values;
            }
        }

        public IDaxObject this[int index]
        {
            get
            {
                return InternalList[index];
            }
        }

        public bool ContainsKey(IDaxObject key)
        {
            return InternalDictionary.ContainsKey(key);
        }

        public bool TryGetValue(IDaxObject key, out List<ObjectReference> value)
        {
            return InternalDictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InternalList.GetEnumerator();
        }
        
        public IEnumerator<KeyValuePair<IDaxObject, List<ObjectReference>>> GetEnumerator()
        {
            return ((IReadOnlyDictionary<IDaxObject, List<ObjectReference>>)InternalDictionary).GetEnumerator();
        }
        #endregion
    }

    public class ReferencedByList : HashSet<IDaxDependantObject>
    {
        public IEnumerable<Measure> Measures { get { return this.OfType<Measure>(); } }
        public IEnumerable<CalculatedColumn> Columns { get { return this.OfType<CalculatedColumn>(); } }
        public IEnumerable<CalculatedTable> Tables { get { return this.OfType<CalculatedTable>(); } }
    }

    public enum DAXProperty
    {
        Expression = 0,
        DetailRowsExpression = 1,
        TargetExpression = 2,
        StatusExpression = 3,
        TrendExpression = 4
    }

    public struct ObjectReference
    {
        public DAXProperty property;
        public int from;
        public int to;
        public bool fullyQualified;

    }

    internal static class DependencyHelper
    {
        static public void AddDep(this IDaxDependantObject target, IDaxObject dependsOn, DAXProperty property, int fromChar, int toChar, bool fullyQualified)
        {
            target.DependsOn.Add(dependsOn, property, fromChar, toChar, fullyQualified);
            if (!dependsOn.ReferencedBy.Contains(target)) dependsOn.ReferencedBy.Add(target);
        }

        /// <summary>
        /// Removes qualifiers such as ' ' and [ ] around a name.
        /// </summary>
        static public string NoQ(this string objectName, bool table = false)
        {
            if (table)
            {
                return objectName.StartsWith("'") ? objectName.Substring(1, objectName.Length - 2) : objectName;
            }
            else
            {
                return objectName.StartsWith("[") ? objectName.Substring(1, objectName.Length - 2) : objectName;
            }
        }
    }


}
