﻿//---------------------------------------------------------------------
// <copyright file="ODataJsonLightSerializerTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.OData.JsonLight;
using Microsoft.OData.Edm;
using Xunit;

namespace Microsoft.OData.Tests.JsonLight
{
    /// <summary>
    /// Unit tests and short-span integration tests for ODataJsonLightSerializer.
    /// </summary>
    public class ODataJsonLightSerializerTests
    {
        [Fact]
        public void WritePayloadStartWritesJsonPaddingStuffIfSpecified()
        {
            var result = SetupSerializerAndRunTest("functionName", serializer => serializer.WritePayloadStart());
            Assert.StartsWith("functionName(", result);
        }

        [Fact]
        public void WritePayloadEndWritesClosingParenthesisIfJsonPaddingSpecified()
        {
            var result = SetupSerializerAndRunTest("functionName", serializer =>
            {
                serializer.WritePayloadStart();
                serializer.WritePayloadEnd();
            });

            Assert.EndsWith(")", result);
        }

        [Fact]
        public void WillNotWriteJsonPaddingStuffIfUnspecified()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                serializer.WritePayloadStart();
                serializer.WritePayloadEnd();
            });

            Assert.DoesNotContain("(", result);
            Assert.DoesNotContain(")", result);
        }

        [Fact]
        public void WriteTopLevelErrorUsesProvidedErrorCode()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError { ErrorCode = "Error Code" };
                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"code\":\"Error Code\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorUsesProvidedMessage()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError { Message = "error message text" };
                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"message\":\"error message text\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorUsesProvidedTarget()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError { Target = "error target text" };
                serializer.WriteTopLevelError(error, includeDebugInformation: false);
            });

            Assert.Contains("\"target\":\"error target text\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorHasCorrectDefaults()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"code\":\"\"", result);
            Assert.Contains("\"message\":\"\"", result);
            Assert.DoesNotContain("\"target\"", result);
            Assert.DoesNotContain("\"details\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithStringInstanceAnnotation()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.primitive", new ODataPrimitiveValue("stringValue"));
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"@sample.primitive\":\"stringValue\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithDateTimeInstanceAnnotation()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var primitiveValue = new ODataPrimitiveValue(new DateTimeOffset(2000, 1, 1, 12, 30, 0, new TimeSpan()));
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.primitive", primitiveValue);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"sample.primitive@odata.type\":\"#DateTimeOffset\",\"@sample.primitive\":\"2000-01-01T12:30:00Z\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithDateInstanceAnnotation()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var primitiveValue = new ODataPrimitiveValue(new Date(2014, 8, 8));
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.primitive", primitiveValue);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"sample.primitive@odata.type\":\"#Date\",\"@sample.primitive\":\"2014-08-08\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithTimeOfDayInstanceAnnotation()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var primitiveValue = new ODataPrimitiveValue(new TimeOfDay(12, 30, 5, 90));
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.primitive", primitiveValue);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"sample.primitive@odata.type\":\"#TimeOfDay\",\"@sample.primitive\":\"12:30:05.0900000\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithStringInstanceAnnotationWithTypeNameAttribute()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var primitiveValue = new ODataPrimitiveValue("stringValue");
                primitiveValue.TypeAnnotation = new ODataTypeAnnotation("Custom.Type");
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.primitive", primitiveValue);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"sample.primitive@odata.type\":\"#Custom.Type\",\"@sample.primitive\":\"stringValue\"", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithResourceInstanceAnnotation()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var resourceValue = new ODataResourceValue();
                resourceValue.TypeName = "ns.ErrorDetails";
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.complex", resourceValue);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"@sample.complex\":{\"@odata.type\":\"#ns.ErrorDetails\"}", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithResourceInstanceAnnotationNoTypeNameShouldThrow()
        {
            SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var resourceValue = new ODataResourceValue();
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.complex", resourceValue);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                Action writeError = () => serializer.WriteTopLevelError(error, false);
                writeError.Throws<ODataException>(Strings.WriterValidationUtils_MissingTypeNameWithMetadata);
            });
        }

        [Fact]
        public void WriteTopLevelErrorWithCollectionOfResourceInstanceAnnotation()
        {
            var result = SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var collection = new ODataCollectionValue
                {
                    TypeName = "Collection(ns.ErrorDetails)",
                    Items = new[] { new ODataResourceValue(), new ODataResourceValue { TypeName = "ns.ErrorDetails" } }
                };
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.collection", collection);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                serializer.WriteTopLevelError(error, false);
            });

            Assert.Contains("\"sample.collection@odata.type\":\"#Collection(ns.ErrorDetails)\",\"@sample.collection\":[{},{}]", result);
        }

        [Fact]
        public void WriteTopLevelErrorWithCollectionOfResourceInstanceAnnotationWithNoTypeNameShouldThrow()
        {
            SetupSerializerAndRunTest(null, serializer =>
            {
                ODataError error = new ODataError();
                var instanceAnnotations = new Collection<ODataInstanceAnnotation>();
                var collection = new ODataCollectionValue
                {
                    Items = new[] { new ODataResourceValue(), new ODataResourceValue { TypeName = "ns.ErrorDetails" } }
                };
                ODataInstanceAnnotation annotation = new ODataInstanceAnnotation("sample.collection", collection);
                instanceAnnotations.Add(annotation);
                error.InstanceAnnotations = instanceAnnotations;

                Action writeError = () => serializer.WriteTopLevelError(error, false);
                writeError.Throws<ODataException>(Strings.WriterValidationUtils_MissingTypeNameWithMetadata);
            });
        }

        [Fact]
        public void UrlToStringShouldThrowWithNoMetadataAndMetadataDocumentUriIsNotProvided()
        {
            var stream = new MemoryStream();
            var serializer = GetSerializer(stream, null, true, false);
            var uri = new Uri("TestUri", UriKind.Relative);
            Action uriToStrongError = () => serializer.UriToString(uri);
            uriToStrongError.Throws<ODataException>(Strings.ODataJsonLightSerializer_RelativeUriUsedWithoutMetadataDocumentUriOrMetadata(UriUtils.UriToString(uri)));
        }

        [Fact]
        public void UrlToStringShouldReturnAbsoluteUriWithNoMetadata()
        {
            var stream = new MemoryStream();
            var serializer = GetSerializer(stream, null, true);
            string uri = serializer.UriToString(new Uri("TestUri", UriKind.Relative));
            Assert.Equal("http://example.com/TestUri", uri);
        }

        [Fact]
        public void UrlToStringShouldReturnRelativeUriWithMinimalMetadata()
        {
            var stream = new MemoryStream();
            var serializer = GetSerializer(stream);
            serializer.JsonWriter.StartObjectScope();
            serializer.WriteContextUriProperty(ODataPayloadKind.ServiceDocument);
            string uri = serializer.UriToString(new Uri("TestUri", UriKind.Relative));
            Assert.Equal("TestUri", uri);
        }

        /// <summary>
        /// Sets up a ODataJsonLightSerializer, runs the given test code, and then flushes and reads the stream back as a string for
        /// customized verification.
        /// </summary>
        private static string SetupSerializerAndRunTest(string jsonpFunctionName, Action<ODataJsonLightSerializer> action)
        {
            var stream = new MemoryStream();
            var serializer = GetSerializer(stream, jsonpFunctionName);
            action(serializer);
            serializer.JsonWriter.Flush();
            stream.Position = 0;
            var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        private static ODataJsonLightSerializer GetSerializer(Stream stream, string jsonpFunctionName = null, bool nometadata = false, bool setMetadataDocumentUri = true)
        {
            var model = new EdmModel();
            var complexType = new EdmComplexType("ns", "ErrorDetails");
            //var collectionType = new EdmCollectionType(new EdmComplexTypeReference(complexType, false));
            model.AddElement(complexType);

            var settings = new ODataMessageWriterSettings { JsonPCallback = jsonpFunctionName, EnableMessageStreamDisposal = false, Version = ODataVersion.V4 };
            if (setMetadataDocumentUri)
            {
                settings.SetServiceDocumentUri(new Uri("http://example.com"));
            }
            var mediaType = nometadata ? new ODataMediaType("application", "json", new KeyValuePair<string, string>("odata", "none")) : new ODataMediaType("application", "json");
            var mainModel = TestUtils.WrapReferencedModelsToMainModel(model);
            var messageInfo = new ODataMessageInfo
            {
                MessageStream = stream,
                MediaType = mediaType,
#if NETCOREAPP1_0
                Encoding = Encoding.GetEncoding(0),
#else
                Encoding = Encoding.Default,
#endif
                IsResponse = true,
                IsAsync = false,
                Model = mainModel,
            };
            var context = new ODataJsonLightOutputContext(messageInfo, settings);
            return new ODataJsonLightSerializer(context, setMetadataDocumentUri);
        }
    }
}
