﻿using System;
using System.Reflection;

namespace BinarySerialization.Graph.TypeGraph
{
    internal abstract class CollectionTypeNode : ContainerTypeNode
    {
        protected CollectionTypeNode(TypeNode parent, Type type) : base(parent, type)
        {
            object terminationValue;
            TerminationChild = GetTerminationChild(out terminationValue);
            TerminationValue = terminationValue;
        }

        protected CollectionTypeNode(TypeNode parent, MemberInfo memberInfo) : base(parent, memberInfo)
        {
            object terminationValue;
            TerminationChild = GetTerminationChild(out terminationValue);
            TerminationValue = terminationValue;
        }

        public Type ChildType { get; set; }

        public TypeNode Child { get; set; }

        public TypeNode TerminationChild { get; private set; }

        public object TerminationValue { get; private set; }

        private TypeNode GetTerminationChild(out object terminationValue)
        {
            if (SerializeUntilAttribute == null)
            {
                terminationValue = null;
                return null;
            }

            terminationValue = SerializeUntilAttribute == null ? null : SerializeUntilAttribute.ConstValue ?? (byte)0;

            TypeNode terminationChild = null;
            if (terminationValue != null)
                terminationChild = GenerateChild(terminationValue.GetType());

            return terminationChild;
        }
    }
}
