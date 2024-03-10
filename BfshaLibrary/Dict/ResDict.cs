using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BfshaLibrary
{
    public class ResDict<T> :  Dictionary<string, T>, IResData where T : IResData, new()
    {
        private List<Node> _nodes = new List<Node>();

        internal List<Node> GetNodes() => _nodes;

        public ResDict() { }

        public T this[int index]
        {
            get
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (i ==  index)
                        return this[GetKey(i)];
                }
                return new T();
            }
        }

        public string GetKey(int index)
        {
            if (index >= 0 && index < Keys.Count)
                return Keys.ElementAt(index);

            return null;
        }

        public void Read(BinaryReader reader)
        {
            reader.ReadUInt32(); //magic
            int numNodes = reader.ReadInt32();

            _nodes.Clear();

            int i = 0;
            for (; numNodes >= 0; numNodes--)
            {
                _nodes.Add(new Node()
                {
                    Reference = reader.ReadUInt32(),
                    IdxLeft = reader.ReadUInt16(),
                    IdxRight = reader.ReadUInt16(),
                    Key = reader.ReadStringOffset(reader.ReadUInt64()),
                });
                i++;
            }

            for (int j = 1; j < _nodes.Count; j++)
                this.Add(_nodes[j].Key, new T());
        }

        public void GenerateTree()
        {
            // Update the Patricia trie values in the nodes.
            var newNodes = ResDictUpdate.UpdateNodes(Keys.ToList());
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].Reference = newNodes[i].Reference;
                _nodes[i].IdxLeft = newNodes[i].IdxLeft;
                _nodes[i].IdxRight = newNodes[i].IdxRight;
                _nodes[i].Key = newNodes[i].Key;
            }
        }

        internal class Node
        {
            internal uint Reference;
            internal ushort IdxLeft;
            internal ushort IdxRight;
            internal string Key;
            internal IResData Value;
        }
    }
}
