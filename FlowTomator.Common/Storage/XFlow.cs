using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace FlowTomator.Common
{
    public class XFlow : EditableFlow
    {
        public static XFlow Load(XDocument document)
        {
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();
            List<Variable> variables = new List<Variable>();

            try
            {
                // Load references
                XElement referenceElements = document.Root.Element("References");
                if (referenceElements != null)
                {
                    foreach (XElement referenceElement in referenceElements.Elements())
                    {
                        switch (referenceElement.Name.LocalName)
                        {
                            case "Assembly":
                                Assembly assembly = Assembly.LoadFile(referenceElement.Attribute("Path").Value);
                                break;

                            case "Script":
                                throw new NotSupportedException();
                        }
                    }
                }

                // Cache assemblies
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type[] nodeTypes = assemblies.SelectMany(a => a.GetTypes())
                                             .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Node)))
                                             .ToArray();

                // Create variables
                XElement variableElements = document.Root.Element("Variables");

                // Create nodes
                XElement nodeElements = document.Root.Element("Nodes");
                foreach (XElement nodeElement in nodeElements.Elements())
                {
                    Type nodeType = nodeTypes.FirstOrDefault(t => t.Name == nodeElement.Name.LocalName);
                    int nodeId = -1;

                    Node node = Activator.CreateInstance(nodeType) as Node;
                    Variable[] nodeInputs = node.Inputs.ToArray();
                    Variable[] nodeOutputs = node.Outputs.ToArray();

                    foreach (XAttribute nodeAttribute in nodeElement.Attributes())
                    {
                        string variableName = nodeAttribute.Name.LocalName;

                        if (variableName == "Id")
                        {
                            if (!int.TryParse(nodeAttribute.Value, out nodeId))
                                throw new FormatException();

                            continue;
                        }

                        Variable inputVariable = nodeInputs.FirstOrDefault(v => v.Name == variableName);
                        Variable outputVariable = nodeOutputs.FirstOrDefault(v => v.Name == variableName);

                        if (inputVariable != null)
                        {
                            if (nodeAttribute.Value.StartsWith("$"))
                            {
                                variableName = nodeAttribute.Value.Substring(1);

                                Variable boundVariable = variables.FirstOrDefault(v => v.Name == variableName);
                                if (boundVariable == null)
                                    variables.Add(boundVariable = new Variable(variableName, inputVariable.Type));

                                inputVariable.Link(boundVariable);
                            }
                            else if (inputVariable.Type != typeof(object))
                            {
                                object value = nodeAttribute.Value;

                                try
                                {
                                    TypeConverter converter = TypeDescriptor.GetConverter(inputVariable.Type);
                                    value = converter.ConvertFromString(nodeAttribute.Value);
                                }
                                catch
                                {
                                    try
                                    {
                                        value = Activator.CreateInstance(inputVariable.Type, nodeAttribute.Value);
                                    }
                                    catch { }
                                }

                                inputVariable.Value = value;
                            }
                            else
                                inputVariable.Value = nodeAttribute.Value;
                        }
                        else if (outputVariable != null)
                        {
                            if (nodeAttribute.Value.StartsWith("$"))
                            {
                                variableName = nodeAttribute.Value.Substring(1);

                                Variable boundVariable = variables.FirstOrDefault(v => v.Name == variableName);
                                if (boundVariable == null)
                                    variables.Add(boundVariable = new Variable(variableName, outputVariable.Type));

                                outputVariable.Link(boundVariable);
                            }
                        }
                        else
                            throw new KeyNotFoundException("Could not find variable " + variableName + " in task " + nodeType.Name);
                    }

                    nodes.Add(nodeId, node);
                }

                // Fill slots
                XElement slotElements = document.Root.Element("Slots");
                foreach (XElement slotElement in slotElements.Elements())
                {
                    int nodeId = -1;
                    int slotIndex = 0;

                    XAttribute nodeIdAttribute = slotElement.Attribute("Id");
                    if (nodeIdAttribute == null || !int.TryParse(nodeIdAttribute.Value, out nodeId))
                        throw new FormatException("Could not find the node specified in this slot");
                    if (!nodes.ContainsKey(nodeId))
                        throw new FormatException("Could not find the node specified in this slot");

                    Node node = nodes[nodeId];
                    Slot[] nodeSlots = node.Slots.ToArray();

                    XAttribute slotIndexAttribute = slotElement.Attribute("Index");
                    if (slotIndexAttribute != null)
                    {
                        if (!int.TryParse(slotIndexAttribute.Value, out slotIndex))
                            throw new FormatException("Could not find the task specified in this slot");
                    }
                    if (slotIndex >= nodeSlots.Length)
                        throw new FormatException("Could not find the task specified in this slot");

                    Slot nodeSlot = nodeSlots[slotIndex];

                    foreach (XElement targetNodeElement in slotElement.Elements("Node"))
                    {
                        int targetNodeId = -1;

                        XAttribute targetNodeIdAttribute = targetNodeElement.Attribute("Id");
                        if (targetNodeIdAttribute == null || !int.TryParse(targetNodeIdAttribute.Value, out targetNodeId))
                            throw new FormatException("Could not find the target node specified in this slot");
                        if (!nodes.ContainsKey(targetNodeId))
                            throw new FormatException("Could not find the target node specified in this slot");

                        nodeSlot.Nodes.Add(nodes[targetNodeId]);
                    }
                }

                // Load editor information
                XElement editionElement = document.Root.Element("Edition");
                foreach (XElement nodeElement in editionElement.Elements("Node"))
                {
                    XAttribute idAttribute = nodeElement.Attribute("Id"),
                               xAttribute = nodeElement.Attribute("X"),
                               yAttribute = nodeElement.Attribute("Y");

                    int id = -1;
                    if (idAttribute == null || !int.TryParse(idAttribute.Value, out id))
                        throw new Exception("Could not find the specified node");

                    double x = id * 100, y = id * 100;
                    if (xAttribute != null)
                        double.TryParse(xAttribute.Value, out x);
                    if (yAttribute != null)
                        double.TryParse(yAttribute.Value, out y);

                    // Prepare node info
                    Node node = nodes.First(p => p.Key == id).Value;
                    node.Metadata.Add("Position.X", x);
                    node.Metadata.Add("Position.Y", y);
                }
            }
            catch (Exception e)
            {
                return null;
            }

            XFlow flow = new XFlow();

            foreach (Node node in nodes.Values)
                flow.Nodes.Add(node);

            return flow;
        }
        public static XDocument Save(XFlow flow)
        {
            XElement nodesElement, slotsElement, editionElement;

            XDocument document = new XDocument(
                new XElement("Flow",
                    nodesElement = new XElement("Nodes"),
                    slotsElement = new XElement("Slots"),
                    editionElement = new XElement("Edition")
                )
            );
            
            for (int i = 0; i < flow.Nodes.Count; i++)
            {
                Node node = flow.Nodes[i];

                XElement nodeElement = new XElement(node.GetType().Name,
                    new XAttribute("Id", i)
                );

                foreach (Variable input in node.Inputs)
                {
                    if (input.Linked != null)
                        nodeElement.Add(new XAttribute(input.Name, "$" + input.Linked.Name));
                    else if (input.Value != input.DefaultValue)
                        nodeElement.Add(new XAttribute(input.Name, input.Value));
                }

                foreach (Variable output in node.Outputs)
                {
                    if (output.Linked != null)
                        nodeElement.Add(new XAttribute(output.Name, "$" + output.Linked.Name));
                }

                nodesElement.Add(nodeElement);

                Slot[] slots = node.Slots.ToArray();
                for (int j = 0; j < slots.Length; j++)
                {
                    if (slots[j].Nodes.Count == 0)
                        continue;

                    XElement slotElement = new XElement("Slot",
                        new XAttribute("Id", i)
                    );

                    if (slots.Length > 1)
                        slotElement.Add(new XAttribute("Index", j));

                    foreach (Node subNode in slots[j].Nodes)
                    {
                        int id = flow.Nodes.IndexOf(subNode);
                        slotElement.Add(new XElement("Node", new XAttribute("Id", id)));
                    }

                    slotsElement.Add(slotElement);
                }
            }

            for (int i = 0; i < flow.Nodes.Count; i++)
            {
                Node node = flow.Nodes[i];

                XElement nodeElement = new XElement("Node",
                    new XAttribute("Id", i)
                );

                object x, y;
                node.Metadata.TryGetValue("Position.X", out x);
                node.Metadata.TryGetValue("Position.Y", out y);

                if (x == null || y == null)
                    continue;

                nodeElement.Add(new XAttribute("X", x));
                nodeElement.Add(new XAttribute("Y", y));

                editionElement.Add(nodeElement);
            }

            return document;
        }
    }
}