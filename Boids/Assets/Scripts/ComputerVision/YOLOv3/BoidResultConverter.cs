using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

namespace BoidVision
{
    public class BoidResultConverter : JsonConverter
    {
        private readonly Type[] _types;

        public BoidResultConverter(params Type[] types)
        {
            _types = types;
        }

        public override bool CanConvert(Type objectType)
        {
            return _types.Any(t => t == objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var resultObj = (BoidVisionClient.ResultJson) value;

            writer.WriteStartObject();
            writer.WritePropertyName("BoidID");
            serializer.Serialize(writer, resultObj.BoidID);
            writer.WritePropertyName("PackageID");
            serializer.Serialize(writer, resultObj.PackageID);
            writer.WritePropertyName("BoidImages");
            serializer.Serialize(writer, resultObj.BoidImages);
            writer.WritePropertyName("BoidClasses");
            serializer.Serialize(writer, resultObj.BoidClasses);
            writer.WritePropertyName("Timestamp");
            serializer.Serialize(writer, resultObj.Timestamp);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            BoidVisionClient.ResultJson result = new BoidVisionClient.ResultJson();
            bool receive_packageID = false;
            bool receive_boidImg = false;
            bool receive_boidClass = false;
            bool receive_timestamp = false;
            bool receive_boidID = false;

            while (reader.Read())
            {
                if (reader.TokenType != JsonToken.PropertyName)
                    break;

                var propertyName = (string)reader.Value;
                if (!reader.Read())
                    continue;

                switch (propertyName) {
                    case "BoidID":
                        result.PackageID = serializer.Deserialize<int>(reader);
                        receive_boidID = true;
                        break;

                    case "PackageID":
                        result.PackageID = serializer.Deserialize<int>(reader);
                        receive_packageID = true;
                        break;

                    case "BoidImage":
                        result.BoidImages = serializer.Deserialize<List<List<float>>>(reader);
                        receive_boidImg = true;
                        break;

                    case "BoidClasses":
                        result.BoidClasses = serializer.Deserialize<List<int>>(reader);
                        receive_boidClass = true;
                        break;

                    case "Timestamp":
                        result.Timestamp = serializer.Deserialize<float>(reader);
                        receive_timestamp = true;
                        break;

                    default:
                        throw new Exception("Invalid ResultJson object; Unknown property name: " + propertyName);
                }
            }

            if (!(receive_timestamp && receive_boidClass && receive_boidImg && receive_packageID && receive_boidID))
            {
                throw new Exception("Invalid ResultJson object");
            }

            return result;
        }
    }
}