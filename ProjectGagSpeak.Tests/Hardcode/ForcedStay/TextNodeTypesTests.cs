using System.Diagnostics.CodeAnalysis;
using GagSpeak.Hardcore.ForcedStay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProjectGagSpeak.Tests.Hardcode.ForcedStay;

public class TextNodeTypesTests { }

public class ConcreteNodeConverterTests
{
    public class CanConvertTests
    {
        [Fact]
        void GivenATextEntryNodeType_WhenCanConvertIsCalled_THenItShouldReturnTrue()
        {
            // Arrange
            var sut = new ConcreteNodeConverter();

            // Act
            var res = sut.CanConvert(typeof(TextEntryNode));

            // Assert
            Assert.True(res);
        }

        [Fact]
        void GivenAChambersTextNodeType_WhenCanConvertIsCalled_THenItShouldReturnTrue()
        {
            // Arrange
            var sut = new ConcreteNodeConverter();

            // Act
            var res = sut.CanConvert(typeof(ChambersTextNode));

            // Assert
            Assert.True(res);
        }

        [Fact]
        void GivenATextFolderNodeType_WhenCanConvertIsCalled_THenItShouldReturnTrue()
        {
            // Arrange
            var sut = new ConcreteNodeConverter();

            // Act
            var res = sut.CanConvert(typeof(TextFolderNode));

            // Assert
            Assert.True(res);
        }

        [Fact]
        void GivenAString_WhenCanConvertIsCalled_THenItShouldReturnFalse()
        {
            // Arrange
            var sut = new ConcreteNodeConverter();

            // Act
            var res = sut.CanConvert(typeof(string));

            // Assert
            Assert.False(res);
        }
    }

    public class WriteJsonTests
    {
        const string TextEntryNodeSimpleName = "GagSpeak.Hardcore.ForcedStay.TextEntryNode, ProjectGagSpeak";
        const string ChambersTextNodeSimpleName = "GagSpeak.Hardcore.ForcedStay.ChambersTextNode, ProjectGagSpeak";
        const string TextFolderNodeSimpleName = "GagSpeak.Hardcore.ForcedStay.TextFolderNode, ProjectGagSpeak";

        [Fact]
        void GivenATextEntryNode_WhenObjectIsSerialized_ThenItShouldIncludeATypeProperty() =>
            TestITextNode(GivenTextEntryNode(), TextEntryNodeSimpleName);

        [Fact]
        void GivenAChambersTextNode_WhenObjectIsSerialized_ThenItShouldIncludeATypeProperty() =>
            TestITextNode(GivenChambersTextNode(), ChambersTextNodeSimpleName);

        [Fact]
        void GivenATextFolderNode_WhenObjectIsSerialized_ThenItShouldIncludeATypeProperty() =>
            TestITextNode(GivenTextFolderNode(), TextFolderNodeSimpleName);

        [Fact]
        void GivenATextFolderNodeWithChilderen_WhenObjectIsSerialized_ThenTheChildrenShouldHaveAType()
        {
            // Arrange
            var textEntryNode = GivenTextFolderNode();
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);

            // Act
            sut.WriteJson(jsonWriter, textEntryNode, serializer);
            var res = stringWriter.ToString();

            // Assert
            var jObject = JObject.Parse(res);

            Assert.True(jObject.TryGetValue("Children", out JToken jType));
            Assert.IsType<JArray>(jType);
            var array = (JArray)jType;
            var first = array[0];
            var second = array[1];

            Assert.IsType<JObject>(first);
            Assert.IsType<JObject>(second);
            var firstObject = (JObject)first;
            var secondObject = (JObject)second;

            Assert.True(firstObject.TryGetValue("$type", out JToken jFirstType));
            Assert.True(secondObject.TryGetValue("$type", out JToken jSecondType));

            Assert.Equal(jFirstType, ChambersTextNodeSimpleName);
            Assert.Equal(jSecondType, TextEntryNodeSimpleName);
        }

        private void TestITextNode(ITextNode textNode, string expectedType)
        {
            // Arrange
            var textEntryNode = textNode;
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);

            // Act
            sut.WriteJson(jsonWriter, textEntryNode, serializer);
            var res = stringWriter.ToString();

            // Assert
            var jObject = JObject.Parse(res);

            Assert.True(jObject.TryGetValue("$type", out JToken jType));
            Assert.Equal(expectedType, jType.Value<string>());
        }

        private TextEntryNode GivenTextEntryNode() => new TextEntryNode()
        {
            Enabled = true,
            FriendlyName = "FriendlyName",
            TargetRestricted = true,
            TargetNodeName = "TargetNodeName",
            TargetNodeLabel = "TargetNodeLabel",
            SelectedOptionText = "SelectedOptionText",
        };

        private TextFolderNode GivenTextFolderNode() => new TextFolderNode()
        {
            Enabled = true,
            FriendlyName = "FriendlyName",
            TargetRestricted = true,
            TargetNodeName = "TargetNodeName",
            Children =
            {
                GivenChambersTextNode(),
                GivenTextEntryNode()
            }
        };

        private ChambersTextNode GivenChambersTextNode() => new ChambersTextNode()
        {
            Enabled = true,
            FriendlyName = "FriendlyName",
            TargetRestricted = true,
            TargetNodeName = "TargetNodeName",
            ChamberRoomSet = 0,
            ChamberListIdx = 0,
        };
    }

    public class ReadJsonTests
    {
        [Fact]
        void GivenJsonOfATextEntryNode_WhenReadJsonIsCalled_ThenItShouldReturnATextEntryNode()
        {
            // Arrange
            var json = """
                       {
                           "$type": "GagSpeak.Hardcore.ForcedStay.TextEntryNode, ProjectGagSpeak",
                           "Enabled": false,
                           "FriendlyName": "(Friendly Name)",
                           "TargetRestricted": true,
                           "TargetNodeName": "",
                           "TargetNodeLabel": "",
                           "TargetNodeLabelIsRegex": false,
                           "SelectedOptionText": ""
                       }
                       """;
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var res = sut.ReadJson(jsonReader, typeof(string), null, serializer);

            // Expect
            Assert.IsType<TextEntryNode>(res);
        }

        [Fact]
        void GivenJsonOfAChambersTextNode_WhenReadJsonIsCalled_ThenItShouldReturnAChambersTextNode()
        {
            // Arrange
            var json = """
                       {
                           "$type": "GagSpeak.Hardcore.ForcedStay.ChambersTextNode, ProjectGagSpeak",
                           "Enabled": true,
                           "FriendlyName": "[ForcedStay] Select FC Chamber Room (2/3)",
                           "TargetRestricted": true,
                           "TargetNodeName": "Entrance to Additional Chambers",
                           "ChamberRoomSet": 0,
                           "ChamberListIdx": 0
                       }
                       """;
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var res = sut.ReadJson(jsonReader, typeof(string), null, serializer);

            // Expect
            Assert.IsType<ChambersTextNode>(res);
        }

        [Fact]
        void GivenJsonOfATextFolderNode_WhenReadJsonIsCalled_ThenItShouldReturnATextFolderNode()
        {
            // Arrange
            var json = """
                       {
                           "$type": "GagSpeak.Hardcore.ForcedStay.TextFolderNode, ProjectGagSpeak",
                           "Enabled": true,
                           "FriendlyName": "ForcedDeclineList",
                           "TargetRestricted": false,
                           "TargetNodeName": "",
                           "Children": [
                               {
                                   "$type": "GagSpeak.Hardcore.ForcedStay.TextEntryNode, ProjectGagSpeak",
                                   "Enabled": true,
                                   "FriendlyName": "[ForcedStay] Prevent Apartment Leaving",
                                   "TargetRestricted": true,
                                   "TargetNodeName": "Exit",
                                   "TargetNodeLabel": "",
                                   "TargetNodeLabelIsRegex": false,
                                   "SelectedOptionText": "Cancel"
                               }
                           ]
                       }
                       """;
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var res = sut.ReadJson(jsonReader, typeof(string), null, serializer);

            // Expect
            Assert.IsType<TextFolderNode>(res);
            var folderNode = (TextFolderNode)res;
            Assert.IsType<TextEntryNode>(folderNode.Children[0]);
        }

        [Fact]
        void
            GivenJsonOfATextFolderNodeWithNestedTextFolderNodes_WhenReadJsonIsCalled_ThenItShouldReturnATextFolderNode()
        {
            // Arrange
            var json = """
                       {
                           "$type": "GagSpeak.Hardcore.ForcedStay.TextFolderNode, ProjectGagSpeak",
                           "Enabled": true,
                           "FriendlyName": "ForcedDeclineList",
                           "TargetRestricted": false,
                           "TargetNodeName": "",
                           "Children": [
                               {
                                   "$type": "GagSpeak.Hardcore.ForcedStay.TextFolderNode, ProjectGagSpeak",
                                   "Enabled": true,
                                   "FriendlyName": "ForcedDeclineList",
                                   "TargetRestricted": false,
                                   "TargetNodeName": "",
                                   "Children": [
                                       {
                                           "$type": "GagSpeak.Hardcore.ForcedStay.TextEntryNode, ProjectGagSpeak",
                                           "Enabled": true,
                                           "FriendlyName": "[ForcedStay] Prevent Apartment Leaving",
                                           "TargetRestricted": true,
                                           "TargetNodeName": "Exit",
                                           "TargetNodeLabel": "",
                                           "TargetNodeLabelIsRegex": false,
                                           "SelectedOptionText": "Cancel"
                                       }
                                   ]
                               }
                           ]
                       }
                       """;
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var res = sut.ReadJson(jsonReader, typeof(string), null, serializer);

            // Expect
            Assert.IsType<TextFolderNode>(res);
            var folderNode = (TextFolderNode)res;
            Assert.IsType<TextFolderNode>(folderNode.Children[0]);
            var folderNodeNested = (TextFolderNode)folderNode.Children[0];
            Assert.IsType<TextEntryNode>(folderNodeNested.Children[0]);
        }
        
        [Fact]
        void GivenAnOutdatedJsonTextFolder_WhenReadJsonIsCalled_ThenItShouldReturnATextFolderNode()
        {
            // Arrange
            var json = """
                       {
                           "Enabled": true,
                           "FriendlyName": "ForcedDeclineList",
                           "TargetRestricted": false,
                           "TargetNodeName": "",
                           "Children": [
                               {
                                   "Enabled": true,
                                   "FriendlyName": "[ForcedStay] Prevent Apartment Leaving",
                                   "TargetRestricted": true,
                                   "TargetNodeName": "Exit",
                                   "TargetNodeLabel": "",
                                   "TargetNodeLabelIsRegex": false,
                                   "SelectedOptionText": "Cancel"
                               }
                           ]
                       }
                       """;
            var sut = new ConcreteNodeConverter();
            var serializer = new JsonSerializer();
            var jsonReader = new JsonTextReader(new StringReader(json));

            // Act
            var res = sut.ReadJson(jsonReader, typeof(string), null, serializer);

            // Expect
            Assert.IsType<TextFolderNode>(res);
            var folderNode = (TextFolderNode)res;
            Assert.IsType<TextEntryNode>(folderNode.Children[0]);
        }
    }
}
