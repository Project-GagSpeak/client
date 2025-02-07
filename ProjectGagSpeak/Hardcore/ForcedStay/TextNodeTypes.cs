using GagSpeak.Localization;
using NAudio.Wave;
using System.Text.RegularExpressions;

namespace GagSpeak.Hardcore.ForcedStay;

public interface ITextNode
{
    /// <summary>
    /// If the TextNode is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The Friendly name of the node.
    /// </summary>
    public string FriendlyName { get; set; }

    /// <summary>
    /// If the Node is restricted to a specific target node.
    /// </summary>
    public bool TargetRestricted { get; set; }

    /// <summary>
    /// The Name of the target node to look for if we are target restricted
    /// </summary>
    public string TargetNodeName { get; set; }
}

public class TextEntryNode : ITextNode
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The Friendly name of the node.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// If the Node is restricted to a specific target node.
    /// </summary>
    public bool TargetRestricted { get; set; } = false;
    
    /// <summary>
    /// The Name of the target node to look for if we are target restricted
    /// </summary>
    public string TargetNodeName { get; set; } = string.Empty;

    /// <summary>
    /// The Prompt Text that the Node provides when we interact with it.
    /// <para>If string is empty, will accept any text for this node</para>
    /// </summary>
    public string TargetNodeLabel { get; set; } = string.Empty;

    public bool TargetNodeLabelIsRegex => TargetNodeLabel.StartsWith("/") && TargetNodeLabel.EndsWith("/");

    [JsonIgnore]
    public Regex? TargetNodeTextRegex
    {
        get
        {
            try
            {
                return new(TargetNodeLabel.Trim('/'), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The option within the prompt that we should automatically select.
    /// </summary>
    public string SelectedOptionText { get; set; } = "No";

    [JsonIgnore]
    public bool IsTextRegex => SelectedOptionText.StartsWith("/") && SelectedOptionText.EndsWith("/");

    [JsonIgnore]
    public Regex? TextRegex
    {
        get
        {
            try
            {
                return new(SelectedOptionText.Trim('/'), RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch
            {
                return null;
            }
        }
    }
}

public class ChambersTextNode : ITextNode
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The Friendly name of the node.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// If the Node is restricted to a specific target node.
    /// </summary>
    public bool TargetRestricted { get; set; } = false;

    /// <summary>
    /// The Name of the target node to look for if we are target restricted
    /// </summary>
    public string TargetNodeName { get; set; } = string.Empty;

    /// <summary>
    /// Which Chamber Room Set to select, if we should (001-015 ext)
    /// 0 == First Selection
    /// </summary>
    public int ChamberRoomSet { get; set; } = 0;

    /// <summary>
    /// The index in the list to select. This is NOT the room number, 
    /// this is the index from top to bottom in the room listings.
    /// </summary>
    public int ChamberListIdx { get; set; } = 0;
}

[Serializable]
public class TextFolderNode : ITextNode
{
    public bool Enabled { get; set; } = true;
    public string FriendlyName { get; set; } = string.Empty;
    public bool TargetRestricted { get; set; } = false;
    public string TargetNodeName { get; set; } = string.Empty;


    [JsonProperty(ItemConverterType = typeof(ConcreteNodeConverter))]
    public List<ITextNode> Children { get; } = new();

    // helper function to prune any empty entires
    public void PruneEmpty()
    {
        // remove any child enrty where the type is a TextEntryNode and it has empty text
        Children.RemoveAll(x => x is TextEntryNode folder && folder.SelectedOptionText == string.Empty);
    }

    public void CheckAndInsertRequired()
    {
        // if there are no entries in the children with the text "Leave Estate" with selection "no" and "Leave Private Chambers" with selection "nothing.",
        var presetNodes = new List<ITextNode>()
        {
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.LeaveAPTFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.LeaveAPTName,
                TargetNodeLabel = "",
                SelectedOptionText = GSLoc.Settings.ForcedStay.LeaveAPTOption,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.LeaveChamberFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.LeaveChamberName,
                TargetNodeLabel = GSLoc.Settings.ForcedStay.LeaveChamberLabel,
                SelectedOptionText = GSLoc.Settings.ForcedStay.LeaveChamberOption,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.LeaveEstateFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.LeaveEstateName,
                TargetNodeLabel = GSLoc.Settings.ForcedStay.LeaveEstateLabel,
                SelectedOptionText = GSLoc.Settings.ForcedStay.LeaveEstateOption,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterEstateFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterEstateName,
                TargetNodeLabel = GSLoc.Settings.ForcedStay.EnterEstateLabel,
                SelectedOptionText = GSLoc.Settings.ForcedStay.EnterEstateOption,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterAPTOneFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterAPTOneName,
                TargetNodeLabel = "",
                SelectedOptionText = GSLoc.Settings.ForcedStay.EnterAPTOneOption,
            },
            new ChambersTextNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterAPTTwoFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterAPTTwoName,
                ChamberRoomSet = 0,
                ChamberListIdx = 0,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterAPTThreeFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterAPTThreeName,
                TargetNodeLabel = GSLoc.Settings.ForcedStay.EnterAPTThreeLabel,
                SelectedOptionText = GSLoc.Settings.ForcedStay.EnterAPTThreeOption,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterFCOneFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterFCOneName,
                TargetNodeLabel = "",
                SelectedOptionText = GSLoc.Settings.ForcedStay.EnterFCOneOption,
            },
            new ChambersTextNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterFCTwoFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterFCTwoName,
                ChamberRoomSet = 0,
                ChamberListIdx = 0,
            },
            new TextEntryNode()
            {
                Enabled = true,
                FriendlyName = GSLoc.Settings.ForcedStay.EnterFCThreeFriendly,
                TargetRestricted = true,
                TargetNodeName = GSLoc.Settings.ForcedStay.EnterFCThreeName,
                TargetNodeLabel = GSLoc.Settings.ForcedStay.EnterFCThreeLabel,
                SelectedOptionText = GSLoc.Settings.ForcedStay.EnterFCThreeOption,
            }
        };

        // If there are no entries, add all preset nodes
        if (Children.Count == 0)
        {
            foreach (var node in presetNodes)
            {
                Children.Add(node);
            }
        }
        else
        {
            // Check each of the first 9 nodes and replace if any properties don't match
            for (int i = 0; i < presetNodes.Count; i++)
            {
                if (i >= Children.Count)
                {
                    // If there are fewer than 9 children, add the missing ones
                    Children.Add(presetNodes[i]);
                }
                else
                {
                    var childNode = Children[i];
                    var presetNode = presetNodes[i];

                    // Check if FriendlyName, TargetNodeName, or TargetNodeLabel don't match
                    if (childNode.Enabled != presetNode.Enabled
                    ||  childNode.FriendlyName != presetNode.FriendlyName 
                    ||  childNode.TargetNodeName != presetNode.TargetNodeName 
                    || (childNode is TextEntryNode textNode && presetNode is TextEntryNode presetTextNode && textNode.TargetNodeLabel != presetTextNode.TargetNodeLabel))
                    {
                        // Replace the mismatching node with the correct preset node
                        Children[i] = presetNode;
                    }
                }
            }
        }
    }
}

// the class to handle the custom serialization to file
public class ConcreteNodeConverter : JsonConverter
{
    public override bool CanRead => true;

    public override bool CanWrite => true;

    public override bool CanConvert(Type objectType) => objectType.IsAssignableTo(typeof(ITextNode));

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        // Needs to be optional for backwards compatibility
        var jType = jObject["$type"]?.Value<string>();

        if (jType == SimpleName(typeof(TextEntryNode)))
        {
            return CreateObject<TextEntryNode>(jObject, serializer);
        }
        if (jType == SimpleName(typeof(ChambersTextNode)))
        {
            return CreateObject<ChambersTextNode>(jObject, serializer);
        }
        if (jType == SimpleName(typeof(TextFolderNode)))
        {
            return CreateObject<TextFolderNode>(jObject, serializer);
        }

        if (jObject["Children"] != null)
        {
            return CreateObject<TextFolderNode>(jObject, serializer);
        }
        
        return CreateObject<TextEntryNode>(jObject, serializer);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var jObject = JObject.FromObject(value!, serializer);
        jObject.AddFirst(new JProperty("$type", SimpleName(value!.GetType())));
        jObject.WriteTo(writer);
    }
    private static T CreateObject<T>(JObject jObject, JsonSerializer serializer) where T : new()
    {
        var obj = new T();
        serializer.Populate(jObject.CreateReader(), obj);
        return obj;
    }

    private static string SimpleName(Type type)
    {
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}
