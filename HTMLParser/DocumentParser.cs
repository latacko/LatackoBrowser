using System;
using System.Diagnostics;
using System.Text;
using HTMLParser.DocumentElements;
using HTMLParser.Enums;
using NodeBehaviour;

namespace HTMLParser;

public class DocumentParser
{
    Document document = new Document();
    int documentLength;
    ReadOnlyMemory<char> documentMemory;

    DomElement? currentElement = null;
    readonly Stack<DomElement> chierarchy = new Stack<DomElement>(16);
    int startOfSentence = -1;

    readonly bool debug = false;
    #region Node variables
    CurrentToken currentToken = CurrentToken.Unknown;

    string attributeKey = "";
    char attributeChar = '"';

    bool willHaveChildren = false;
    bool closedNode = false;
    #endregion

    public DocumentParser(string document, bool debug = false)
    {
        this.debug = debug;
        this.document = new Document();
        ScanDocument(document);
    }

    public void ResetVariables()
    {
        currentToken = CurrentToken.Unknown;
        attributeKey = "";
        attributeChar = '"';

        willHaveChildren = false;
        closedNode = false;
    }

    public Document Get()
    {
        return document;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    void Print(string msg)
    {
        if (!debug) return;

        Console.WriteLine(msg);
    }

    void ScanDocument(string document)
    {
        documentMemory = document.AsMemory();
        ReadOnlySpan<char> documentSpan = documentMemory.Span;

        documentLength = documentSpan.Length;

        for (int i = 0; i < documentLength; i++)
        {
            var _char = documentSpan[i];
            switch (currentToken)
            {
                case CurrentToken.ContextComment:
                    if (_char == '>' && i >= 2 && documentSpan[i - 1] == '-' && documentSpan[i - 2] == '-')
                    {
                        currentElement.Childrens.Add(new CommentElement()
                        {
                            Content = documentSpan[startOfSentence..(i - 2)].ToString()
                        });
                        ResetVariables();
                    }
                    break;
                default:
                    if (_char == '<') OpeningTagChar(i);
                    else if (currentToken != CurrentToken.Unknown)
                        GeneratingNode(i, _char);
                    break;
            }
            // if (currentToken != CurrentToken.ContextComment && _char == '<')
            //     OpeningTagChar(i);
            // else if (currentToken == CurrentToken.ContextComment)
            //     GeneratingComment(i);
            // else if (currentToken != CurrentToken.Unknown)
            //     GeneratingNode(i, _char);
        }
    }

    void OpeningTagChar(int i)
    {
        ReadOnlySpan<char> documentSpan = documentMemory.Span;

        if (currentToken == CurrentToken.Context)
        {
            if (currentElement.Childrens == null)
                currentElement.SetChildrenList();

            currentElement.Childrens.Add(new StringElement()
            {
                Content = documentSpan[startOfSentence..i].ToString()
            });

            currentToken = CurrentToken.Unknown;
            Print("Element with content: " + currentElement);
        }
        // if (willHaveChildren)
        // {
        //     if (_chierarchy.Peek().Childrens == null)
        //         _chierarchy.Peek().SetChildrenList();
        //     _chierarchy.Peek().Childrens.Add(currentElement);
        // }
        ResetVariables();
        willHaveChildren = true;
        currentToken = CurrentToken.Tag;
        startOfSentence = i + 1;
        Print("============================================================================");
        Print("Opening new node and scaning tag:");
    }

    void GeneratingNode(int i, char character)
    {
        ReadOnlySpan<char> documentSpan = documentMemory.Span;

        if (currentToken == CurrentToken.Context)
        {
            return;
        }

        //? Dzięki temu skipujemy wszystkie znaki aż nie znajdziemy znaku zamknięcia node'a 
        if (closedNode && character != '>')
        {
            return;
        }

        if (currentToken == CurrentToken.Tag)
        {
            GeneratingTag(i, character);
        }
        else if (character == '>')
        {
            var _nodeBehaviour = PredefinedNodesBehaviour.Get(currentElement.NodeName);
            SetUpNode(i, _nodeBehaviour);
        }
        else if (currentToken == CurrentToken.Attribute && (character == '=' || char.IsWhiteSpace(character)))
        {
            if (startOfSentence == i)
            {
                startOfSentence++;
                return;
            }
            attributeKey = documentSpan[startOfSentence..i].ToString();
            Print("Opening attribute content for " + attributeKey + ":");
            if (character == '=')
            {
                currentToken = CurrentToken.AttributeContent;
                startOfSentence = i + 1;
                if (documentSpan[i + 1] == '"' || documentSpan[i + 1] == '\'')
                {
                    attributeChar = documentSpan[i + 1];
                }
            }
            else
            {
                currentElement.AddAttribute(attributeKey, "");
                startOfSentence = i + 1;
            }
        }
        else if (currentToken == CurrentToken.AttributeContent && startOfSentence != i && (character == attributeChar))
        {
            GeneratingAttribute(i, character);
        }
        else if (currentToken != CurrentToken.AttributeContent && character == '/')
        {
            Print("Closed node");
            closedNode = true;
        }
    }

    void GeneratingTag(int i, char character)
    {
        ReadOnlySpan<char> documentSpan = documentMemory.Span;
        if (char.IsWhiteSpace(character) || character == '>')
        {
            Print("Done char: |" + character + "|");
            if (documentSpan[i - 1] == '/')
            {
                Print("Closed node");
                closedNode = true;
                willHaveChildren = false;
            }


            if (documentSpan[startOfSentence] == '/')
            {
                Print("Closing node " + chierarchy.Count);
                if (documentSpan[(startOfSentence + 1)..i].SequenceEqual(chierarchy.Peek().NodeName))
                {
                    var _element = chierarchy.Pop();
                    Print("Removing " + _element.NodeName + " from hierarchy final form" + _element);
                    if (chierarchy.Count > 0 && PredefinedNodesBehaviour.Get(chierarchy.Peek().NodeName).CanHaveChildren)
                    {
                        currentElement = chierarchy.Peek();
                        Print("New current element " + currentElement.NodeName);
                        ResetVariables();
                        currentToken = CurrentToken.Context;
                        startOfSentence = i + 1;
                    }
                    else
                    {
                        ResetVariables();
                    }
                }
                else
                {
                    ResetVariables();
                }
                return;
            }

            if (documentSpan.Length >= 3 && documentSpan[startOfSentence] == '!' && documentSpan[startOfSentence + 1] == '-' && documentSpan[startOfSentence + 2] == '-')
            {
                Print("Comment");
                ResetVariables();
                currentToken = CurrentToken.ContextComment;
                startOfSentence = i;
                return;
            }
            string tagName = documentSpan[startOfSentence..i].ToString();

            var _nodeBehaviour = PredefinedNodesBehaviour.Get(tagName);
            currentElement = _nodeBehaviour.GetElement();
            currentElement.SetNodeName(tagName);
            Print("Creating type: " + currentElement.GetType() + " tag: " + tagName);


            if (char.IsWhiteSpace(character))
            {
                Print("Finding attribute:");
                currentToken = CurrentToken.Attribute;
                startOfSentence = i + 1;
            }
            else
            {
                Print("Node done.");
                SetUpNode(i, _nodeBehaviour);
            }
        }
    }

    void GeneratingComment(int i)
    {
        ReadOnlySpan<char> documentSpan = documentMemory.Span;

        if (documentSpan[i] == '>' && i >= 2 && documentSpan[i - 1] == '-' && documentSpan[i - 2] == '-')
        {

            return;
        }
    }

    void SetUpNode(int i, NodeBehaviourInfo nodeBehaviourInfo)
    {
        ReadOnlySpan<char> documentSpan = documentMemory.Span;

        if (currentToken == CurrentToken.Attribute && startOfSentence != i)
        {
            Print("Adding attribute " + documentSpan[startOfSentence..i].ToString() + "|");
            currentElement.AddAttribute(documentSpan[startOfSentence..i].ToString(), "");
        }

        if (!nodeBehaviourInfo.CanHaveChildren)
            willHaveChildren = false;

        if (currentElement.NodeName[0] == '/' || nodeBehaviourInfo.AutoClose)
            willHaveChildren = false;

        bool _scanForContent = false;

        if (chierarchy.Count == 0)
            this.document.Elements.Add(currentElement);
        else
        {
            var _element = chierarchy.Peek();
            if (_element.Childrens == null)
                _element.SetChildrenList();
            _element.Childrens.Add(currentElement);
        }

        Print("Adding " + currentElement);
        if (willHaveChildren)
        {
            Print("Adding to hierarchy");
            chierarchy.Push(currentElement!);

            _scanForContent = true;
        }
        else
        {
            currentElement = null;
            if (chierarchy.Count > 0)
            {
                currentElement = chierarchy.Peek();
                _scanForContent = true;
            }
        }
        ResetVariables();

        if (_scanForContent)
        {
            currentToken = CurrentToken.Context;
            Print("Scaning for context");
        }
        startOfSentence = i + 1;
    }

    void GeneratingAttribute(int i, char character)
    {
        ReadOnlySpan<char> documentSpan = documentMemory.Span;
        Print("Closing attribute content for " + attributeKey + ":" + documentSpan[startOfSentence..(i + 1)].ToString());
        currentToken = CurrentToken.Attribute;
        if (documentSpan[startOfSentence] == documentSpan[i])
        {
            if ((startOfSentence + 1) == i)
                currentElement.AddAttribute(attributeKey, "");
            else
                currentElement.AddAttribute(attributeKey, documentSpan[(startOfSentence + 1)..(i - 1)].ToString());
        }
        else
            currentElement.AddAttribute(attributeKey, documentSpan[startOfSentence..i].ToString());
        startOfSentence = i + 1;
    }
}
