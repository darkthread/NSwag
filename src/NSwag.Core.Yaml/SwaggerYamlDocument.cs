﻿//-----------------------------------------------------------------------
// <copyright file="SwaggerYamlDocument.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Infrastructure;
using NJsonSchema.Yaml;
using YamlDotNet.Serialization;

namespace NSwag
{
    /// <summary>Extension methods to load and save <see cref="SwaggerDocument"/> from/to YAML.</summary>
    public static class SwaggerYamlDocument
    {
        /// <summary>Creates a Swagger specification from a YAML string.</summary>
        /// <param name="data">The JSON or YAML data.</param>
        /// <returns>The <see cref="SwaggerDocument"/>.</returns>
        public static Task<SwaggerDocument> FromYamlAsync(string data)
        {
            return FromYamlAsync(data, null, SchemaType.Swagger2, null);
        }

        /// <summary>Creates a Swagger specification from a YAML string.</summary>
        /// <param name="data">The JSON or YAML data.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <returns>The <see cref="SwaggerDocument"/>.</returns>
        public static Task<SwaggerDocument> FromYamlAsync(string data, string documentPath)
        {
            return FromYamlAsync(data, documentPath, SchemaType.Swagger2, null);
        }

        /// <summary>Creates a Swagger specification from a YAML string.</summary>
        /// <param name="data">The JSON or YAML data.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="expectedSchemaType">The expected schema type which is used when the type cannot be determined.</param>
        /// <returns>The <see cref="SwaggerDocument"/>.</returns>
        public static Task<SwaggerDocument> FromYamlAsync(string data, string documentPath, SchemaType expectedSchemaType)
        {
            return FromYamlAsync(data, documentPath, expectedSchemaType, null);
        }

        /// <summary>Creates a Swagger specification from a YAML string.</summary>
        /// <param name="data">The JSON or YAML data.</param>
        /// <param name="documentPath">The document path (URL or file path) for resolving relative document references.</param>
        /// <param name="expectedSchemaType">The expected schema type which is used when the type cannot be determined.</param>
        /// <param name="referenceResolverFactory">The JSON reference resolver factory.</param>
        /// <returns>The <see cref="SwaggerDocument"/>.</returns>
        public static async Task<SwaggerDocument> FromYamlAsync(string data, string documentPath, SchemaType expectedSchemaType, Func<SwaggerDocument, JsonReferenceResolver> referenceResolverFactory)
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(data));
            var serializer = new SerializerBuilder()
                .JsonCompatible()
                .Build();

            var json = serializer.Serialize(yamlObject);

            referenceResolverFactory = referenceResolverFactory ?? CreateReferenceResolverFactory();
            return await SwaggerDocument.FromJsonAsync(json, documentPath, expectedSchemaType, referenceResolverFactory).ConfigureAwait(false);
        }

        /// <summary>Converts the Swagger specification to YAML.</summary>
        /// <returns>The YAML string.</returns>
        public static string ToYaml(this SwaggerDocument document)
        {
            var json = document.ToJson();
            var expConverter = new ExpandoObjectConverter();
            dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);

            var serializer = new Serializer();
            return serializer.Serialize(deserializedObject);
        }

        /// <summary>Creates a Swagger specification from a JSON file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The <see cref="SwaggerDocument" />.</returns>
        public static async Task<SwaggerDocument> FromFileAsync(string filePath)
        {
            var data = await DynamicApis.FileReadAllTextAsync(filePath).ConfigureAwait(false);
            return await FromYamlAsync(data, filePath).ConfigureAwait(false);
        }

        /// <summary>Creates a Swagger specification from an URL.</summary>
        /// <param name="url">The URL.</param>
        /// <returns>The <see cref="SwaggerDocument"/>.</returns>
        public static async Task<SwaggerDocument> FromUrlAsync(string url)
        {
            var data = await DynamicApis.HttpGetAsync(url).ConfigureAwait(false);
            return await FromYamlAsync(data, url).ConfigureAwait(false);
        }

        private static Func<SwaggerDocument, JsonReferenceResolver> CreateReferenceResolverFactory()
        {
            return document =>
            {
                var schemaResolver = new SwaggerSchemaResolver(document, new JsonSchemaGeneratorSettings());
                return new JsonAndYamlReferenceResolver(schemaResolver);
            };
        }
    }
}
