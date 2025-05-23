// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities.Extensions
{
    [Serializable]
    public abstract class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        [HideInInspector]
        private List<TKey> keyData = new List<TKey>();

        [SerializeField]
        [HideInInspector]
        private List<TValue> valueData = new List<TValue>();

        private bool keyConflict;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (keyConflict) { return; }

            keyData.Clear();
            valueData.Clear();

            foreach (var (key, value) in this)
            {
                keyData.Add(key);
                valueData.Add(value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            keyConflict = false;
            Clear();

            for (var i = 0; i < keyData.Count && i < valueData.Count; i++)
            {
                if (!TryAdd(keyData[i], valueData[i]))
                {
                    keyConflict = true;
                }
            }
        }
    }
}
