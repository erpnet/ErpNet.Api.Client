using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Threading;

namespace ErpNet.Api.Client
{
    /// <summary>
    /// Provides methods to read a JSON stream into dictionary or list.
    /// </summary>
    public static class JsonHelper
    {
        class ObjectContext
        {
            public List<object?>? List;
            public Dictionary<string, object?>? Dictionary;
            public string? CurrentProperty;
            public void AddValue(object? value)
            {
                if (List != null)
                    List.Add(value);
                else if (Dictionary != null)
                    Dictionary[CurrentProperty ?? "?"] = value;
            }
        }

        /// <summary>
        /// Reads an odata query response stream. For each item in the response the specified action is called.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="readItem"></param>
        public static void ReadODataQueryResult(this Stream stream, Action<IDictionary<string, object?>> readItem)
        {
            JsonStreamReader reader = new JsonStreamReader(stream);
            if (reader.IsEmpty)
                return;

            Exception UnexpectedJsonToken(JsonTokenType tokenType)
            {
                return new Exception($"Unexpected JSON token: {tokenType}");
            }
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw UnexpectedJsonToken(reader.TokenType);


            while (reader.Reader.TokenStartIndex < 8000)
            {
                reader.Read();
                
                if (reader.TokenType == JsonTokenType.PropertyName && reader.Reader.ValueTextEquals("value"))
                {
                    // Read the StartArray token.
                    reader.Read();
                    while (true)
                    {
                        bool success;
                        // Whe the EndArray token is reached the success flag will be set to false
                        var item = ReadJsonObject(stream, ref reader, out success);
                        if (!success)
                            break;
                        if (item is IDictionary<string, object?> dict)
                            readItem(dict);
                    }
                    break;
                }
            }




        }

        /// <summary>
        /// Reads a JSON stream and returns, List{object?} for arrays, Dictionary{string,object?} for objects and double, string, null, true or false for primitives.
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <param name="obj">The result object</param>
        /// <returns></returns>
        public static bool TryReadJsonObject(this Stream stream, out object? obj)
        {
            obj = null;
            var reader = new JsonStreamReader(stream);

            if (reader.IsEmpty)
                return false;
            obj = ReadJsonObject(stream,ref reader, out var success);
            return success;
        }

        /// <summary>
        /// Parses the JSON string into an object. For JSON arrays a List{object?} is returned; for JSON objects a Dictionary{string,object?}; or else false, true, string, double or null.
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static object? Parse(string jsonString)
        {
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                JsonElement root = document.RootElement;

                object? GetElementObject(JsonElement el)
                {
                    switch (el.ValueKind)
                    {
                        case JsonValueKind.Array:
                            {
                                List<object?> list = new List<object?>();
                                foreach (var child in el.EnumerateArray())
                                    list.Add(GetElementObject(child));
                                return list;
                            }
                        case JsonValueKind.False:
                            return false;
                        case JsonValueKind.Null:
                            return null;
                        case JsonValueKind.Number:
                            return el.GetDouble();
                        case JsonValueKind.Object:
                            {
                                Dictionary<string, object?> dict = new Dictionary<string, object?>();
                                foreach (var entry in el.EnumerateObject())
                                    dict[entry.Name] = GetElementObject(entry.Value);
                                return dict;
                            }
                        case JsonValueKind.String:
                            return el.GetString();
                        case JsonValueKind.True:
                            return true;

                        case JsonValueKind.Undefined:
                        default:
                            throw new Exception($"Unexpected json element kind {el.ValueKind}.");
                    }
                }

                return GetElementObject(root);
            }
        }

        /// <summary>
        /// Gets the value for a multipart identifier. Example dictionary.Member("Customer.Party.PartyName").
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="dataMember"></param>
        /// <returns></returns>
        public static object? Member(this IDictionary<string, object?> dict, string dataMember)
        {
            var path = dataMember.Split('.');
            IDictionary<string, object?>? d = dict;
            object? value = null;
            for (int i = 0; i < path.Length; i++)
            {
                if (d == null)
                    return null;
                var key = path[i];
                if (!d.TryGetValue(key, out value))
                    break;

                d = value as IDictionary<string, object?>;
            }
            return value;
        }
        /// <summary>
        /// Gets the value for a multipart identifier. Example dictionary.Member("Customer.Party.PartyName").
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="dataMember"></param>
        /// <returns></returns>
        public static T? Member<T>(this IDictionary<string, object?> dict, string dataMember)
        {
            return (T?)Member(dict, dataMember);
        }

        /// <summary>
        /// Reads an object from JSON stream. 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static object? ReadJsonObject(this Stream stream)
        {
            object? obj;
            var reader = new JsonStreamReader(stream);
            if (reader.IsEmpty)
                return false;
            obj = ReadJsonObject(stream, ref reader, out var success);
            if (!success)
                throw new InvalidOperationException($"Failed to read JSON stream. Last JSON buffer:\r\n {Encoding.UTF8.GetString(reader.CurrentBuffer)}");
            return obj;
        }

       

        private static object? ReadJsonObject(Stream stream, ref JsonStreamReader reader, out bool success)
        {
            success = true;
            Stack<ObjectContext> context = new Stack<ObjectContext>();
            while (reader.Read())
            {
                var current = context.Count > 0 ? context.Peek() : null;

                switch (reader.TokenType)
                {
                    case JsonTokenType.StartArray:
                        context.Push(
                            new ObjectContext
                            {
                                List = new List<object?>()
                            });
                        break;
                    case JsonTokenType.StartObject:
                        context.Push(
                            new ObjectContext
                            {
                                Dictionary = new Dictionary<string, object?>()
                            });
                        break;
                    case JsonTokenType.PropertyName:
                        if (current != null)
                            current.CurrentProperty = GetStringValue(reader);
                        break;
                    case JsonTokenType.EndObject:
                        if (context.Count == 0)
                        {
                            success = false;
                            return null;
                        }
                        var obj = context.Pop();
                        current = context.Count > 0 ? context.Peek() : null;
                        if (current != null)
                            current.AddValue(obj.Dictionary);
                        else
                            return obj.Dictionary;
                        break;
                    case JsonTokenType.EndArray:
                        if (context.Count == 0)
                        {
                            success = false;
                            return null;
                        }
                        var array = context.Pop();
                        current = context.Count > 0 ? context.Peek() : null;
                        if (current != null)
                            current.AddValue(array.List);
                        else
                            return array.List;
                        break;
                    case JsonTokenType.False:
                        if (current != null)
                            current.AddValue(false);
                        else
                            return false;
                        break;
                    case JsonTokenType.Null:
                        if (current != null)
                            current.AddValue(null);
                        else
                            return null;
                        break;
                    case JsonTokenType.None:
                        throw new Exception("Unexpected JSON token type: None.");
                    case JsonTokenType.Number:
                        var b = reader.Reader.TryGetDouble(out var num);
                        if (current != null)
                            current.AddValue(num);
                        else
                            return num;
                        break;
                    case JsonTokenType.String:
                        var str = GetStringValue(reader);
                        if (current != null)
                            current.AddValue(str);
                        else
                            return str;
                        break;
                    case JsonTokenType.True:
                        if (current != null)
                            current.AddValue(true);
                        else
                            return true;
                        break;
                }

                if (context.Count == 0)
                    break;
            }
            success = false;
            return null;
        }

        private static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        /// <summary>
        /// Serializes the provided object to JSON.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="writeIndented"></param>
        /// <returns></returns>
        public static string? ToJson(object? resource, bool writeIndented = false)
        {
            if (resource == null)
                return null;
            var opts = CreateJsonSerializerOptions();
            opts.WriteIndented = writeIndented;
            return JsonSerializer.Serialize(resource, resource.GetType(), opts);
        }

        static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-fA-F0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }

        private static string GetStringValue(JsonStreamReader reader)
        {
            string value;
            if (reader.Reader.HasValueSequence)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var span in reader.Reader.ValueSequence)
                {
                    sb.Append(Encoding.UTF8.GetString(span.ToArray()));
                }
                value = sb.ToString();
            }
            else
            {
                value = Encoding.UTF8.GetString(reader.Reader.ValueSpan.ToArray());
            }
            return DecodeEncodedNonAsciiCharacters(value);
        }

        private ref struct JsonStreamReader
        {
            Stream _stream;
            byte[] _buffer;
            ReadOnlySpan<byte> _readerBuffer;
            Utf8JsonReader _reader;
            JsonReaderState _lastValidState;
            int _bytesConsumed;

            public JsonStreamReader(Stream stream)
            {
                _stream = stream;
                _buffer = new byte[4096];
                var bytesRead = stream.Read(_buffer, 0, _buffer.Length);
                _readerBuffer = _buffer.AsSpan(0, bytesRead);
                _reader = new Utf8JsonReader(_readerBuffer, false, default);
                _lastValidState = _reader.CurrentState;
                _bytesConsumed = (int)_reader.BytesConsumed;
            }

            public bool IsEmpty => _readerBuffer.Length == 0;

            public JsonTokenType TokenType => _reader.TokenType;

            public Utf8JsonReader Reader => _reader;

            public byte[] CurrentBuffer => _buffer;

            public bool Read()
            {
                int errorsCount = 0;
                int num = 0;
                // prevent endless cicle
                while (num < 100)
                {
                    num++;
                    bool isError = false;
                    _bytesConsumed = (int)_reader.BytesConsumed;
                    _lastValidState = _reader.CurrentState;
                    try
                    {
                        if (_reader.Read())
                            return true;
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        if (errorsCount > 3)
                            throw;
                        isError = true;
                        errorsCount++;
                    }
                    if (!isError)
                    {
                        _bytesConsumed = (int)_reader.BytesConsumed;
                        _lastValidState = _reader.CurrentState;
                    }
                    //GetMoreBytesFromStream
                    {
                        int bytesRead;
                        if (_bytesConsumed < _readerBuffer.Length)
                        {
                            ReadOnlySpan<byte> leftover;
                            leftover = _buffer.AsSpan(_bytesConsumed, _readerBuffer.Length - _bytesConsumed);
                           
                            // Extend buffer to fit the large token
                            if (leftover.Length == _buffer.Length)
                            {
                                Array.Resize(ref _buffer, _buffer.Length * 2);
                                //Console.WriteLine($"Increased buffer size to {buffer.Length}");
                            }

                            leftover.CopyTo(_buffer);
                            bytesRead = _stream.Read(_buffer, leftover.Length, _buffer.Length - leftover.Length);
                            _readerBuffer = _buffer.AsSpan(0, bytesRead + leftover.Length);
                        }
                        else
                        {
                            bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
                            _readerBuffer = _buffer.AsSpan(0, bytesRead);
                        }
                        //Console.WriteLine($"String in buffer is: {Encoding.UTF8.GetString(buffer)}");
                        _reader = new Utf8JsonReader(_readerBuffer, isFinalBlock: bytesRead == 0, _lastValidState);
                    }
                }
                return false;
            }

        }

       
        /// <summary>
        /// Returns the JSON string representation of the dictionary.
        /// </summary>
        /// <param name="dict">The dictionary</param>
        /// <param name="writeIndented">If null only dictionaries with more than 4 entries are written indented.</param>
        /// <returns></returns>
        public static string ToJson(this IDictionary<string, object?> dict, bool? writeIndented = null)
        {
            var options = CreateJsonSerializerOptions();
            options.WriteIndented = writeIndented ?? dict.Count > 4;
            return JsonSerializer.Serialize(dict, options);
        }
    }
}