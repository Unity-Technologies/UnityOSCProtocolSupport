using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Media.Osc
{
    class ReferenceManager<TKey, TOwner, TClass>
        where TKey : IEquatable<TKey>
        where TOwner : class
        where TClass : class, IDisposable
    {
        struct Reference
        {
            public TClass Instance;
            public List<TOwner> Owners;
        }

        readonly Dictionary<TKey, Reference> m_KeyToReference = new Dictionary<TKey, Reference>();
        readonly Func<TKey, TClass> m_CreateInstance;

        public ReferenceManager(Func<TKey, TClass> createInstance)
        {
            m_CreateInstance = createInstance;
        }

        public IEnumerable<TOwner> GetOwners(TKey key)
        {
            return m_KeyToReference.TryGetValue(key, out var reference) ? reference.Owners : Enumerable.Empty<TOwner>();
        }

        public void GetOrCreate(TKey key, TOwner owner, out TClass instance)
        {
            if (owner == null)
            {
                instance = default;
                return;
            }

            if (m_KeyToReference.TryGetValue(key, out var reference))
            {
                instance = reference.Instance;
                reference.Owners.Add(owner);

                m_KeyToReference[key] = reference;
            }
            else
            {
                instance = m_CreateInstance(key);
                reference = new Reference
                {
                    Instance = instance,
                    Owners = new List<TOwner>
                    {
                        owner,
                    },
                };

                m_KeyToReference.Add(key, reference);
            }
        }

        public bool Release(TKey key, TOwner owner)
        {
            if (owner == null || !m_KeyToReference.TryGetValue(key, out var reference) || !reference.Owners.Remove(owner))
            {
                return false;
            }

            if (reference.Owners.Count == 0)
            {
                reference.Instance.Dispose();
                m_KeyToReference.Remove(key);
                return true;
            }

            m_KeyToReference[key] = reference;
            return false;
        }
    }
}
