using System;
using System.Collections;
using System.Collections.Generic;

namespace Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers
{
    internal sealed class Indices : IEnumerable<uint>, IEquatable<Indices>
    {
        private readonly uint[] _indices;
        private readonly int _hashCode;

        public Indices(IList<uint> indices)
        {
            _indices = new uint[indices.Count];

            // Since they are going to be in a lookup table go ahead and already calculate the hash code.
            // Nothing fancy here, just adding a hash function that takes into account the indices contents.
            var hashCode = indices.Count;
            foreach (var index in indices)
            {
                hashCode = (int)unchecked((hashCode * 314159) + index);
            }

            _hashCode = hashCode;
        }

        public override bool Equals(object obj) => obj is Indices anotherIndices && Equals(anotherIndices);

        public bool Equals(Indices other)
        {
            if (_hashCode != other?._hashCode || _indices.Length != other._indices.Length)
            {
                return false;
            }

            for (var i = 0; i < _indices.Length; i++)
            {
                if (_indices[i] != other._indices[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() => _hashCode;

        public IEnumerator<uint> GetEnumerator() => ((IEnumerable<uint>)_indices).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _indices.GetEnumerator();
    }
}
