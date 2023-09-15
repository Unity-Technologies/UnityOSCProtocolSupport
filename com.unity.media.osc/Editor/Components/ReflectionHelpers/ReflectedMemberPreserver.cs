using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace Unity.Media.Osc.Editor
{
    /// <summary>
    /// Creates a link.xml file to preserve type members required by OscMessageOutput, as it relies on reflection.
    /// </summary>
    class ReflectedMemberPreserver : IUnityLinkerProcessor
    {
        const string k_XmlDirectory = "Library/Osc";
        const string k_XmlName = "OscPreserve.xml";

        int IOrderedCallback.callbackOrder => 0;

        string IUnityLinkerProcessor.GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            var contents = CreateLinkXml();

            var projectDir = Application.dataPath.Replace("/Assets", string.Empty);
            var xmlDir = $"{projectDir}/{k_XmlDirectory}";
            var xmlPath = $"{xmlDir}/{k_XmlName}";

            if (!Directory.Exists(xmlDir))
            {
                Directory.CreateDirectory(xmlDir);
            }

            File.WriteAllText(xmlPath, contents);

            return xmlPath;
        }

        static string CreateLinkXml()
        {
            var membersToPreserve = FindMembersToPreserve();

            var linkXml = new StringBuilder();

            linkXml.AppendLine("<linker>");

            // Sort everything by name so the file output is deterministic. This ensures the build system
            // only detects a change when the preserved members are different.
            foreach (var assemblyMembers in membersToPreserve.OrderBy(a => a.Key.GetName().Name))
            {
                linkXml.AppendLine($"  <assembly fullname=\"{assemblyMembers.Key.GetName().Name}\">");

                foreach (var typeMembers in assemblyMembers.Value.OrderBy(t => t.Key.FullName))
                {
                    linkXml.AppendLine($"    <type fullname=\"{FormatForXml(ToCecilName(typeMembers.Key.FullName))}\">");

                    foreach (var member in typeMembers.Value.OrderBy(m => m.Name))
                    {
                        var memberName = FormatForXml(member.Name);

                        switch (member)
                        {
                            case FieldInfo field:
                            {
                                linkXml.AppendLine($"      <field name=\"{memberName}\" />");
                                break;
                            }
                            case PropertyInfo property:
                            {
                                linkXml.AppendLine($"      <property name=\"{memberName}\" />");
                                break;
                            }
                            case MethodInfo method:
                            {
                                linkXml.AppendLine($"      <method name=\"{memberName}\" />");
                                break;
                            }
                        }
                    }

                    linkXml.AppendLine("    </type>");
                }

                linkXml.AppendLine("  </assembly>");
            }

            linkXml.AppendLine("</linker>");

            return linkXml.ToString();
        }

        static Dictionary<Assembly, Dictionary<Type, HashSet<MemberInfo>>> FindMembersToPreserve()
        {
            var membersToPreserve = new Dictionary<Assembly, Dictionary<Type, HashSet<MemberInfo>>>();

            // search all scenes in the build and prefabs to find any uses of the message output component
            ComponentFinder<OscMessageOutput>.Search(output =>
            {
                foreach (var member in output.GetArgumentMembers())
                {
                    var type = member.DeclaringType;

                    if (type == null)
                    {
                        continue;
                    }

                    var assembly = type.Assembly;

                    if (!membersToPreserve.TryGetValue(assembly, out var typeMembers))
                    {
                        typeMembers = new Dictionary<Type, HashSet<MemberInfo>>();
                        membersToPreserve.Add(assembly, typeMembers);
                    }
                    if (!typeMembers.TryGetValue(type, out var members))
                    {
                        members = new HashSet<MemberInfo>();
                        typeMembers.Add(type, members);
                    }

                    members.Add(member);
                }
            });

            return membersToPreserve;
        }

        static string ToCecilName(string fullTypeName)
        {
            return fullTypeName.Replace('+', '/');
        }

        static string FormatForXml(string value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
