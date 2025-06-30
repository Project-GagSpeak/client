using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace GagSpeak.Game;

public enum NodePosition {
    BeforeTarget,
    AfterTarget,
    BeforeAllSiblings,
    AfterAllSiblings,
    AsLastChild,
    AsFirstChild,
}

/// <summary>
///     Taken from KamiToolkit, modified for personal needs. All credit goes to the original author.
/// </summary>
internal static unsafe class NodeLinker {
    internal static void AttachNode(AtkResNode* node, AtkResNode* attachTargetNode, NodePosition position) {
        switch (position) {
            case NodePosition.BeforeTarget:
                EmplaceBefore(node, attachTargetNode);
                break;

            case NodePosition.AfterTarget:
                EmplaceAfter(node, attachTargetNode);
                break;

            case NodePosition.BeforeAllSiblings:
                EmplaceBeforeSiblings(node, attachTargetNode);
                break;

            case NodePosition.AfterAllSiblings:
                EmplaceAfterSiblings(node, attachTargetNode);
                break;
            
            case NodePosition.AsLastChild:
                EmplaceAsLastChild(node, attachTargetNode);
                break;
            
            case NodePosition.AsFirstChild:
                EmplaceAsFirstChild(node, attachTargetNode);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
        }
    }
    
     private static void EmplaceBefore(AtkResNode* node, AtkResNode* attachTargetNode) {
        node->ParentNode = attachTargetNode->ParentNode;

        // Target node is the head of the nodelist, we will be the new head.
        if (attachTargetNode->NextSiblingNode is null) {
            attachTargetNode->ParentNode->ChildNode = node;
        }

        // We have a node that will be before us
        if (attachTargetNode->NextSiblingNode is not null) {
            attachTargetNode->NextSiblingNode->PrevSiblingNode = node;
            node->NextSiblingNode = attachTargetNode->NextSiblingNode;
        }

        attachTargetNode->NextSiblingNode = node;
        node->PrevSiblingNode = attachTargetNode;

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component) {
            attachTargetNode->ParentNode->ChildCount++;
        }
     }

    private static void EmplaceAfter(AtkResNode* node, AtkResNode* attachTargetNode) {
        node->ParentNode = attachTargetNode->ParentNode;

        // We have a node that will be after us
        if (attachTargetNode->PrevSiblingNode is not null) {
            attachTargetNode->PrevSiblingNode->NextSiblingNode = node;
            node->PrevSiblingNode = attachTargetNode->PrevSiblingNode;
        }

        attachTargetNode->PrevSiblingNode = node;
        node->NextSiblingNode = attachTargetNode;

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component) {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceBeforeSiblings(AtkResNode* node, AtkResNode* attachTargetNode) {
        var current = attachTargetNode;
        var previous = current;

        while (current is not null) {
            previous = current;
            current = current->NextSiblingNode;
        }

        if (previous is not null) {
            EmplaceBefore(node, previous);
        }

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component) {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceAfterSiblings(AtkResNode* node, AtkResNode* attachTargetNode) {
        var current = attachTargetNode;
        var previous = current;

        while (current is not null) {
            previous = current;
            current = current->PrevSiblingNode;
        }

        if (previous is not null) {
            EmplaceAfter(node, previous);
        }

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component) {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceAsLastChild(AtkResNode* node, AtkResNode* attachTargetNode) {
        // If the child list is empty
        if (attachTargetNode->ChildNode is null && attachTargetNode->GetNodeType() is not NodeType.Component)
        {
            if (attachTargetNode->GetNodeType() is not NodeType.Component) {
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else {
                node->ParentNode = attachTargetNode;
            }
        }
        // Else Add to the List
        else
        {
            var currentNode = attachTargetNode->ChildNode;
            while (currentNode is not null && currentNode->PrevSiblingNode != null)
            {
                currentNode = currentNode->PrevSiblingNode;
            }

            node->ParentNode = attachTargetNode;
            node->NextSiblingNode = currentNode;
            currentNode->PrevSiblingNode = node;
            if (attachTargetNode->GetNodeType() is not NodeType.Component) {
                attachTargetNode->ChildCount++;
            }
        }
    }
    
    private static void EmplaceAsFirstChild(AtkResNode* node, AtkResNode* attachTargetNode) {
        // If the child list is empty
        if (attachTargetNode->ChildNode is null && attachTargetNode->ChildCount is 0)
        {
            if (attachTargetNode->GetNodeType() is not NodeType.Component) {
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else {
                node->ParentNode = attachTargetNode;
            }
        }
        // Else Add to the List as the First Child
        else {
            if (attachTargetNode->GetNodeType() is not NodeType.Component) {
                attachTargetNode->ChildNode->NextSiblingNode = node;
                node->PrevSiblingNode = attachTargetNode->ChildNode;
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else {
                node->PrevSiblingNode = attachTargetNode->ChildNode;
                node->ParentNode = attachTargetNode;
            }
        }
    }

    public static void DetachNode(AtkResNode* node) {
        if (node is null) return;
        if (node->ParentNode is null) return;

        if (node->ParentNode->ChildNode == node)
            node->ParentNode->ChildNode = node->PrevSiblingNode;

        if (node->PrevSiblingNode != null)
            node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;

        if (node->NextSiblingNode != null)
            node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
        
        if (node->ParentNode->GetNodeType() is not NodeType.Component) {
            node->ParentNode->ChildCount--;
        }
    }
}
