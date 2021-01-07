using System.Collections.Generic;
using System.Linq;

namespace SignalFx.Tracing.Headers
{
    /// <summary>
    /// Type to wrap an IDictionary&gt;string, Istring&lt; into a
    /// <see cref="IHeadersCollection"/>. Multiple header values are comma
    /// delimited into a single value on the wrapped dictionary.
    /// </summary>
    /// <remarks>
    /// This wrapper is useful to help propagate context in environments
    /// like AWS API Gateway V2 in which multi-values of headers are
    /// separated by commas.
    /// </remarks>
    public class CommaDelimitedValueHeaders : IHeadersCollection
    {
        private readonly IDictionary<string, string> _dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommaDelimitedValueHeaders"/> class.
        /// </summary>
        public CommaDelimitedValueHeaders()
        {
            _dictionary = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommaDelimitedValueHeaders"/> class.
        /// </summary>
        /// <param name="capacity">Initial capacity of the collection.</param>
        public CommaDelimitedValueHeaders(int capacity)
        {
            _dictionary = new Dictionary<string, string>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommaDelimitedValueHeaders"/> class.
        /// </summary>
        /// <param name="dictionary">
        /// Existing dictionary with comma delimited values to be wrapped by the instance.
        /// </param>
        public CommaDelimitedValueHeaders(IDictionary<string, string> dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc/>
        public void Add(string name, string value)
        {
            if (_dictionary.TryGetValue(name, out var existingValue))
            {
                value = existingValue + "," + value;
            }

            _dictionary[name] = value;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetValues(string name)
        {
            return _dictionary.TryGetValue(name, out var values)
                       ? values.Split(',')
                       : Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public void Remove(string name)
        {
            _dictionary.Remove(name);
        }

        /// <inheritdoc/>
        public void Set(string name, string value)
        {
            _dictionary.Remove(name);
            _dictionary.Add(name, value);
        }
    }
}
