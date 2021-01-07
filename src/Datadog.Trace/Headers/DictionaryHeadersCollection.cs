using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalFx.Tracing.Headers
{
    /// <summary>
    /// Type to wrap an IDictionary&gt;string, IList&gt;string&lt;&lt; into a
    /// <see cref="IHeadersCollection"/>.
    /// </summary>
    public class DictionaryHeadersCollection : IHeadersCollection
    {
        private readonly IDictionary<string, IList<string>> _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryHeadersCollection"/> class.
        /// </summary>
        public DictionaryHeadersCollection()
        {
            _headers = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryHeadersCollection"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity of the collection.</param>
        public DictionaryHeadersCollection(int capacity)
        {
            _headers = new Dictionary<string, IList<string>>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryHeadersCollection"/> class.
        /// </summary>
        /// <param name="dictionary">
        /// Existing dictionary to be wrapped by the instance.
        /// </param>
        public DictionaryHeadersCollection(IDictionary<string, IList<string>> dictionary)
        {
            _headers = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetValues(string name)
        {
            return _headers.TryGetValue(name, out var values)
                       ? values
                       : Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public void Set(string name, string value)
        {
            _headers.Remove(name);
            _headers.Add(name, new List<string> { value });
        }

        /// <inheritdoc/>
        public void Add(string name, string value)
        {
            Add(name, new[] { value });
        }

        /// <inheritdoc/>
        public void Remove(string name)
        {
            _headers.Remove(name);
        }

        internal void Add(string name, IEnumerable<string> values)
        {
            if (!_headers.TryGetValue(name, out var list))
            {
                list = new List<string>();
                _headers[name] = list;
            }

            foreach (var value in values)
            {
                list.Add(value);
            }
        }
    }
}
