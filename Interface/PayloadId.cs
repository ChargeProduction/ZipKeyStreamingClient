using System;
using System.Collections.Generic;
using ZipKeyStreamingClient.Interface.Payload;

namespace ZipKeyStreamingClient.Interface
{
    public class PayloadId
    {
        private static Dictionary<Type, int> payloadtoId = new Dictionary<Type, int>();

        static PayloadId()
        {
            RegisterId<CommandPayload>(1);
            RegisterId<CameraDataPayload>(2);
        }

        private static void RegisterId<T>(int id)
        {
            payloadtoId[typeof(T)] = id;
        }
        public static int GetId<T>()
        {
            return GetId(typeof(T));
        }

        public static int GetId(Type type)
        {
            payloadtoId.TryGetValue(type, out var result);
            return result;
        }

        public static Type GetType(int code)
        {
            foreach (var keyValuePair in payloadtoId)
            {
                if (keyValuePair.Value == code)
                {
                    return keyValuePair.Key;
                }
            }

            return null;
        }
    }
}