using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityAutopilot.Utils
{
    public class Json
    {
        public static Json convert = new Json();

        public JsonSerializerSettings SerializeSetting;

        public JsonSerializerSettings DeserializeSetting;

        public Json(Formatting formatting = Formatting.None)
        {
            SerializeSetting = new JsonSerializerSettings();
            DeserializeSetting = new JsonSerializerSettings();

            this.SerializeSetting.ContractResolver = new LowercaseContractResolver();
            this.SerializeSetting.NullValueHandling = NullValueHandling.Ignore;
            this.SerializeSetting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            this.SerializeSetting.Formatting = formatting;
            this.SerializeSetting.Converters.Add(new EnumToLowerStringConverter());
            this.SerializeSetting.Converters.Add(new UnityVector3JsonConverter());

            this.DeserializeSetting.NullValueHandling = NullValueHandling.Ignore;
            this.DeserializeSetting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            this.DeserializeSetting.Converters.Add(new EnumToLowerStringConverter());
            this.DeserializeSetting.Converters.Add(new UnityVector3JsonConverter());
        }


        public string SerializeObject(object obj)
        {
            return JsonConvert.SerializeObject(obj, SerializeSetting);
        }

        public T DeserializeObject<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, DeserializeSetting)!;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"{ex.Message}");
            }
            return default(T)!;
        }


        public class EnumToLowerStringConverter : StringEnumConverter
        {
            public override bool CanConvert(Type objectType)
            {
                Type type = Nullable.GetUnderlyingType(objectType) ?? objectType;
                return type.IsEnum;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                bool isNullable = Nullable.GetUnderlyingType(objectType) != null;
                Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

                if (!enumType.IsEnum)
                    throw new JsonSerializationException($"Type {enumType} is not an enum.");

                if (reader.TokenType == JsonToken.String)
                {
                    string enumText = reader.Value?.ToString();
                    if (Enum.TryParse(enumType, enumText, true, out object result))
                        return result;
                }
                else if (reader.TokenType == JsonToken.Integer)
                {
                    int intValue = Convert.ToInt32(reader.Value);
                    if (Enum.IsDefined(enumType, intValue))
                        return Enum.ToObject(enumType, intValue);
                }

                // For non-nullable enums, return the default value instead of null
                return isNullable ? null : Activator.CreateInstance(enumType);
            }
            
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }
                writer.WriteValue(value.ToString()!.ToLower());
            }
        }

        public class LowercaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                // Use default camelCase
                return base.ResolvePropertyName(propertyName);
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                // If the property is a IDictionary<,> preserve original property name
                var propType = (member as PropertyInfo)?.PropertyType;

                if (propType != null && IsDictionary(propType))
                {
                    property.PropertyName = member.Name; // Use original casing
                }
                else
                {
                    property.PropertyName = member.Name.ToLower();
                }

                return property;
            }

            private bool IsDictionary(Type type)
            {
                if (!type.IsGenericType || !type.IsArray) return false;

                return type.IsAssignableFrom(typeof(IDictionary<,>));
            }
        }

        public class UnityVector3JsonConverter : JsonConverter<Vector3>
        {
            public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WritePropertyName("z");
                writer.WriteValue(value.z);
                writer.WriteEndObject();
            }

            public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                float x = 0, y = 0, z = 0;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject)
                        break;

                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propName = (string)reader.Value;
                        reader.Read();
                        switch (propName)
                        {
                            case "x": x = Convert.ToSingle(reader.Value); break;
                            case "y": y = Convert.ToSingle(reader.Value); break;
                            case "z": z = Convert.ToSingle(reader.Value); break;
                        }
                    }
                }

                return new Vector3(x, y, z);
            }
        }
    }
}