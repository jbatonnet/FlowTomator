using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace FlowTomator.Common
{
    [FlowStorage("FlowTomator automation flow", ".xflow")]
    public class XFlow : EditableFlow
    {
        public static XFlow Load(XDocument document)
        {
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();
            Dictionary<Slot, List<int>> slotNodes = new Dictionary<Slot, List<int>>();
            List<Variable> globalVariables = new List<Variable>();
            List<Variable> localVariables = new List<Variable>();

            XElement propertiesElement = document.Root.Element("Properties");
            XElement nodesElement = document.Root.Element("Properties");

            // Load references
            XElement[] referenceElements = propertiesElement?.Element("References")?.Elements()?.ToArray();
            if (referenceElements != null)
            {
                foreach (XElement referenceElement in referenceElements)
                {
                    switch (referenceElement.Name.LocalName)
                    {
                        case "Assembly":
                            string file = referenceElement.Attribute("Path").Value;
                            string path = file;

                            if (!Path.IsPathRooted(path))
                                path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);

                            try
                            {
                                Assembly assembly = Assembly.LoadFile(path);
                            }
                            catch
                            {
                                throw new Exception("Could not find referenced assembly " + file);
                            }
                            break;

                        case "Script":
                            throw new NotSupportedException();
                    }
                }
            }

            // Load global variables
            XElement[] variableElements = propertiesElement?.Element("Variables")?.Elements()?.ToArray();
            if (variableElements != null)
            {
                foreach (XElement variableElement in variableElements)
                {
                    XAttribute nameAttribute = variableElement.Attribute("Name");
                    XAttribute valueAttribute = variableElement.Attribute("Value");

                    if (nameAttribute == null || valueAttribute == null)
                        throw new Exception("Variable name and/or value are missing in variable at line " + (variableElement as IXmlLineInfo).LineNumber);

                    string name = nameAttribute.Value;

                    if (globalVariables.Any(v => v.Name == name))
                        throw new Exception("There is already a global variable named " + name + " in this flow at line " + (variableElement as IXmlLineInfo).LineNumber);

                    globalVariables.Add(new Variable(name, typeof(object), valueAttribute.Value));
                }
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            // Create nodes
            XElement[] nodeElements = document.Root.Element("Nodes")?.Elements("Node")?.ToArray();
            if (nodeElements != null)
            {
                foreach (XElement nodeElement in nodeElements)
                {
                    XAttribute typeAttribute = nodeElement.Attribute("Type");
                    XAttribute idAttribute = nodeElement.Attribute("Id");

                    if (typeAttribute == null || idAttribute == null)
                        throw new Exception("Node type and/or id are missing in node at line " + (nodeElement as IXmlLineInfo).LineNumber);

                    Type type = assemblies.Select(a => a.GetType(typeAttribute.Value, false))
                                          .Where(t => t != null)
                                          .FirstOrDefault();
                    if (type == null)
                        throw new Exception("Could not find node type " + typeAttribute.Value + " at line " + (typeAttribute as IXmlLineInfo).LineNumber);

                    int id = -1;
                    if (!int.TryParse(idAttribute.Value, out id))
                        throw new Exception("Node id could not be read at line " + (idAttribute as IXmlLineInfo).LineNumber);

                    XElement inputsElement = nodeElement.Element("Inputs");
                    XElement outputsElement = nodeElement.Element("Outputs");
                    XElement metadataElement = nodeElement.Element("Metadata");
                    XElement[] slotsElement = nodeElement.Elements("Slot").ToArray();

                    Node node = Activator.CreateInstance(type) as Node;
                    
                    // Read inputs
                    if (inputsElement != null)
                    {
                        foreach (XAttribute inputAttribute in inputsElement.Attributes())
                        {
                            Variable[] inputs = node.Inputs.ToArray();
                            string name = inputAttribute.Name.LocalName;

                            Variable variable = inputs.FirstOrDefault(i => i.Name == name);
                            if (variable == null)
                            {
                                Log.Warning("Input {0} could not be found in node type at line {1}. Its content will be saved in nodes metadata.", name, (inputAttribute as IXmlLineInfo).LineNumber);
                                node.Metadata["Input." + name] = inputAttribute.Value;
                                continue;
                            }

                            if (inputAttribute.Value.StartsWith("$"))
                            {
                                string link = inputAttribute.Value.Substring(1);

                                Variable linkedVariable = Enumerable.Concat(localVariables, globalVariables).FirstOrDefault(v => v.Name == link);
                                if (linkedVariable == null)
                                    localVariables.Add(linkedVariable = new Variable(link, variable.Type));

                                variable.Link(linkedVariable);
                            }
                            else if (variable.Type == typeof(object) || variable.Type == typeof(string))
                                variable.Value = inputAttribute.Value;
                            else
                            {
                                object value = inputAttribute.Value;

                                try
                                {
                                    variable.Value = value;
                                }
                                catch
                                {
                                    throw new Exception("Could not convert the specified object into the variable " + name + " at line " + (inputAttribute as IXmlLineInfo).LineNumber);
                                }
                            }
                        }
                    }

                    Variable[] outputs = node.Outputs.ToArray();

                    // Read outputs
                    if (outputsElement != null)
                    {
                        foreach (XAttribute outputAttribute in outputsElement.Attributes())
                        {
                            string name = outputAttribute.Name.LocalName;

                            Variable variable = outputs.FirstOrDefault(i => i.Name == name);
                            if (variable == null)
                            {
                                Log.Warning("Output {0} could not be found in node type at line {1}. Its content will be saved in nodes metadata.", name, (outputAttribute as IXmlLineInfo).LineNumber);
                                node.Metadata["Output." + name] = outputAttribute.Value;
                                continue;
                            }

                            if (!outputAttribute.Value.StartsWith("$"))
                                Log.Warning("Output {0} cannot be set to a constant value at line {1}. Its content will be skipped.", name, (outputAttribute as IXmlLineInfo).LineNumber);
                            else
                            {
                                string link = outputAttribute.Value.Substring(1);

                                Variable linkedVariable = Enumerable.Concat(localVariables, globalVariables).FirstOrDefault(v => v.Name == link);
                                if (linkedVariable == null)
                                    localVariables.Add(linkedVariable = new Variable(link, variable.Type));

                                variable.Link(linkedVariable);
                            }
                        }
                    }

                    // Read metadata
                    if (metadataElement != null)
                    {
                        foreach (XAttribute metadataAttribute in metadataElement.Attributes())
                        {
                            string name = metadataAttribute.Name.LocalName;
                            string value = metadataAttribute.Value;

                            int intValue;
                            if (int.TryParse(value, out intValue))
                            {
                                node.Metadata[name] = intValue;
                                continue;
                            }

                            double doubleValue;
                            if (double.TryParse(value, out doubleValue))
                            {
                                node.Metadata[name] = doubleValue;
                                continue;
                            }

                            node.Metadata[name] = value;
                        }
                    }

                    Slot[] slots = node.Slots.ToArray();

                    // Decode slots
                    foreach (XElement slotElement in slotsElement)
                    {
                        XAttribute indexAttribute = slotElement.Attribute("Index");

                        int index = 0;
                        if (indexAttribute != null && !int.TryParse(indexAttribute.Value, out index))
                            throw new Exception("Could not read slot index at line " + (indexAttribute as IXmlLineInfo).LineNumber);

                        if (slots.Length <= index)
                            throw new Exception("Could not find slot " + index + " at line " + (indexAttribute as IXmlLineInfo).LineNumber);

                        Slot slot = slots[index];

                        XElement[] slotNodeElements = slotElement.Elements("Node").ToArray();
                        foreach (XElement slotNodeElement in slotNodeElements)
                        {
                            int slotNodeId = -1;

                            XAttribute slotNodeIdAttribute = slotNodeElement.Attribute("Id");
                            if (slotNodeIdAttribute == null || !int.TryParse(slotNodeIdAttribute.Value, out slotNodeId))
                                throw new FormatException("Could not find the target node specified in slot at line " + (slotNodeElement as IXmlLineInfo).LineNumber);

                            List<int> slotNodeIds;
                            if (!slotNodes.TryGetValue(slot, out slotNodeIds))
                                slotNodes.Add(slot, slotNodeIds = new List<int>());

                            slotNodeIds.Add(slotNodeId);
                        }
                    }

                    nodes.Add(id, node);
                }

                // Resolve slots
                foreach (var pair in slotNodes)
                {
                    Slot slot = pair.Key;

                    foreach (int slotNodeId in pair.Value)
                    {
                        Node node;
                        if (!nodes.TryGetValue(slotNodeId, out node))
                            throw new FormatException("Could not find the target node id " + slotNodeId);

                        slot.Nodes.Add(node);
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

            XFlow flow = new XFlow();

            foreach (Node node in nodes.Values)
                flow.Nodes.Add(node);
            foreach (Variable variable in globalVariables)
                flow.Variables.Add(variable);

            return flow;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyLocation = args.RequestingAssembly.Location;
            string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            string assemblyName = new AssemblyName(args.Name).Name;
            string assemblyPath = Path.Combine(assemblyDirectory, assemblyName + ".dll");

            if (!File.Exists(assemblyPath))
                return null;

            return Assembly.LoadFile(assemblyPath);
        }

        public new static XFlow Load(string path)
        {
            return Load(XDocument.Load(path, LoadOptions.SetLineInfo));
        }

        public static XDocument Save(XFlow flow)
        {
            XElement propertiesElement, nodesElement;

            XDocument document = new XDocument(
                new XElement("Flow",
                    propertiesElement = new XElement("Properties"),
                    nodesElement = new XElement("Nodes")
                )
            );

            // Save references
            Assembly[] assemblies = flow.Nodes.Select(n => Assembly.GetAssembly(n.GetType()))
                                              .Except(new [] { Assembly.GetAssembly(typeof(Flow)) })
                                              .Distinct()
                                              .ToArray();
            if (assemblies.Length > 0)
            {
                XElement referencesElement = new XElement("References");
                propertiesElement.Add(referencesElement);

                string binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                foreach (Assembly assembly in assemblies)
                {
                    string location = assembly.Location;
                    location = Utilities.MakeRelativePath(binaryPath, location);

                    referencesElement.Add(new XElement("Assembly", new XAttribute("Path", location)));
                }
            }

            // Save nodes
            for (int i = 0; i < flow.Nodes.Count; i++)
            {
                Node node = flow.Nodes[i];

                XElement inputsElement, outputsElement, metadataElement;
                XElement nodeElement = new XElement("Node",
                    new XAttribute("Type", node.GetType().FullName),
                    new XAttribute("Id", i),
                    inputsElement = new XElement("Inputs"),
                    outputsElement = new XElement("Outputs"),
                    metadataElement = new XElement("Metadata")
                );

                // Inputs
                foreach (Variable input in node.Inputs)
                {
                    if (input.Linked != null)
                        inputsElement.Add(new XAttribute(input.Name, "$" + input.Linked.Name));
                    else if (input.Value != input.DefaultValue)
                    {
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(input.Type);
                        string value;

                        if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
                            value = typeConverter.ConvertToString(input.Value);
                        else
                            value = input.Value.ToString();

                        inputsElement.Add(new XAttribute(input.Name, value));
                    }
                }

                // Outputs
                foreach (Variable output in node.Outputs)
                {
                    if (output.Linked != null)
                        outputsElement.Add(new XAttribute(output.Name, "$" + output.Linked.Name));
                }

                // Metadata
                foreach (var pair in node.Metadata)
                    metadataElement.Add(new XAttribute(pair.Key, pair.Value.ToString()));

                // Slots
                Slot[] slots = node.Slots.ToArray();
                for (int j = 0; j < slots.Length; j++)
                {
                    if (slots[j].Nodes.Count == 0)
                        continue;

                    XElement slotElement = new XElement("Slot");

                    if (slots.Length > 1)
                        slotElement.Add(new XAttribute("Index", j));

                    foreach (Node subNode in slots[j].Nodes)
                    {
                        int id = flow.Nodes.IndexOf(subNode);
                        slotElement.Add(new XElement("Node", new XAttribute("Id", id)));
                    }

                    nodeElement.Add(slotElement);
                }

                // Clean empty elements
                if (!inputsElement.HasAttributes)
                    inputsElement.Remove();
                if (!outputsElement.HasAttributes)
                    outputsElement.Remove();
                if (!metadataElement.HasAttributes)
                    metadataElement.Remove();

                nodesElement.Add(nodeElement);
            }

            return document;
        }
        public override void Save(string path)
        {
            Save(this).Save(path);
        }
    }
}