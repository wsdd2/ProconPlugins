﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;

using PRoCon.Core;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;

namespace PRoConEvents
{
    public class CInGameAdminEx : PRoConPluginAPI, IPRoConPluginInterface
    {
        private bool m_isPluginEnabled = false;

        #region Menu List

        public const string CommandsHeader = "Commands";
        public const string ResponseScopeHeader = "Response Scope";
        public const string ResponsesHeader = "Responses";


        [Menu(CommandsHeader, "Swap Team")]
        private string m_strSwapCommand = "swap";

        [Menu(CommandsHeader, "Confirm Selection")]
        private string m_strConfirmCommand = "yes";

        [Menu(ResponseScopeHeader, "Private Prefix")]
        private string m_strPrivatePrefix = "@";


        [Menu(ResponseScopeHeader, "Admins Prefix")]
        private string m_strAdminsPrefix = "#";


        [Menu(ResponseScopeHeader, "Public Prefix")]
        private string m_strPublicPrefix = "!";


        [Menu(ResponsesHeader, "Show responses (seconds)")]
        private string m_iShowMessageLength = "swap";



        // Add some code

        private List<CPluginVariable> GetVariables(bool isAddHeader)
        {
            List<CPluginVariable> pluginVariables = new List<CPluginVariable>();

            // Add some code
            pluginVariables.Add(CreateVariable(() => m_strSwapCommand, isAddHeader));
            pluginVariables.Add(CreateVariable(() => m_strPrivatePrefix, isAddHeader));
            pluginVariables.Add(CreateVariable(() => m_strAdminsPrefix, isAddHeader));
            pluginVariables.Add(CreateVariable(() => m_strPublicPrefix, isAddHeader));
            pluginVariables.Add(CreateVariable(() => m_iShowMessageLength, isAddHeader));


            return pluginVariables;
        }

        #endregion

        #region IPRoConPluginInterface

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            return GetVariables(true);
        }

        public void OnPluginDisable()
        {
            Output.Information(string.Format("^b{0} {1} ^1Disabled^n", GetPluginName(), GetPluginVersion()));
            m_isPluginEnabled = false;
            // Add some code
            UnregisterAllCommands(true);
        }

        public void OnPluginEnable()
        {
            Output.Information(string.Format("^b{0} {1} ^2Enabled^n", GetPluginName(), GetPluginVersion()));
            /// Add some code
            m_isPluginEnabled = true;
            RegisterAllCommands();
        }

        private void RegisterAllCommands()
        {
            if (!m_isPluginEnabled) return;

            List<string> emptyList = new List<string>();
            List<string> scopes = Listify<string>(m_strPrivatePrefix, m_strAdminsPrefix, m_strPublicPrefix);
            MatchCommand confirmationCommand = new MatchCommand(scopes, m_strConfirmCommand, Listify<MatchArgumentFormat>());

            RegisterCommand(
                new MatchCommand(
                    ClassName,
                    "OnCommandSwapTeam",
                    scopes,
                    m_strSwapCommand,
                    new List<MatchArgumentFormat>(),
                    new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanMovePlayers | Privileges.CanKillPlayers, 2, confirmationCommand, "You do not have enough privileges to swap team"),
                    "Swap team"
                )
            );

        }

        private void UnregisterAllCommands(bool force)
        {
            if (m_isPluginEnabled == true || force == true)
            {
                List<string> emptyList = new List<string>();

                UnregisterCommand(
                     new MatchCommand(
                         emptyList,
                         m_strSwapCommand,
                         Listify<MatchArgumentFormat>()
                     )
                 );
            }
        }

        public void OnCommandSwapTeam(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            ExecuteCommand("procon.protected.events.write", "Plugins", "PluginAction", String.Format("Initiated a swap team"), strSpeaker);

            List<int> teamIds = FrostbitePlayerInfoList.Values.Select(_ => _.TeamID).Distinct().ToList();
            Output.Information("TeamIds:" + string.Join(",",teamIds));
            for (int i = 0; i < FrostbitePlayerInfoList.Count; i++)
            {
                var player = FrostbitePlayerInfoList.ElementAt(i).Value;
                var name = player.SoldierName;
                var dstTeamId = teamIds[((teamIds.IndexOf(player.TeamID) + 1) < teamIds.Count) ? (teamIds.IndexOf(player.TeamID) + 1) : 0];
                var dstSquadId = player.SquadID;
                ExecuteCommand("procon.protected.tasks.add", ClassName + strSpeaker, 10.ToString(), "1", "1", "procon.protected.send", "admin.movePlayer", name, dstTeamId.ToString(), dstSquadId.ToString(), true.ToString());
                Output.Information("{0} {1} {2} {3} {4}", "admin.movePlayer", name, dstTeamId.ToString(), dstSquadId.ToString(), true.ToString());
            }
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            Output.Listeners.Add(new TextWriterTraceListener(ClassName + "_" + strHostName + "_" + strPort + ".log")); // output to debug file
            Output.Listeners.Add(new PRoConTraceListener(this)); // output to pluginconsole
            Output.AutoFlush = true;

            // Get and register common events in this class and PRoConPluginAPI
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            IEnumerable<string> baseMethods = typeof(PRoConPluginAPI).GetMethods().Where(_ => _.IsVirtual).Select(_ => _.Name);
            IEnumerable<string> commonMethods = GetType().GetMethods(bindingFlags).Where(_ => _.IsVirtual).Select(_ => _.Name).Intersect(baseMethods);
            RegisterEvents(ClassName, commonMethods.ToArray());
            // Add some code
        }

        public string GetPluginAuthor()
        {
            return "IOL0ol1";
        }

        public string GetPluginDescription()
        {
            return "Description";
        }

        public string GetPluginVersion()
        {
            return "0.0.0.1";
        }

        public string GetPluginWebsite()
        {
            return "Website";
        }

        public string GetPluginName()
        {
            return "In-Game Admin Ex";
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetVariables(false);
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            UnregisterAllCommands(false);
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            // search field to set value
            foreach (FieldInfo item in GetType().GetFields(bindingFlags))
            {
                MenuAttribute menu = item.GetCustomAttributes(false).FirstOrDefault(_ => _ is MenuAttribute) as MenuAttribute;
                if (menu != null && menu.Name == strVariable)
                {
                    if (item.IsInitOnly) return; // if it's readonly field, do not set value.
                    object value = strValue;
                    if (item.FieldType.BaseType == typeof(Enum))
                    {
                        value = Enum.Parse(item.FieldType, strValue);
                    }
                    else if (item.FieldType == typeof(string[]))
                    {
                        value = strValue.Split('|');
                    }
                    else
                    {
                        value = Convert.ChangeType(strValue, item.FieldType);
                    }

                    item.SetValue(this, value);
                    return;
                }
            }
        }

        #endregion

        #region Event

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            base.OnListPlayers(players, subset);
            if (!m_isPluginEnabled) return;
            // Add some code.
        }

        #endregion

        #region Private Methods

        private void Command(params string[] args)
        {
            List<string> list = new List<string>
            {
                "procon.protected.send"
            };
            list.AddRange(args);
            ExecuteCommand(list.ToArray());
        }

        private CPluginVariable CreateVariable<T>(Expression<Func<T>> exp, bool isAddHeader)
        {
            /// only valid for remote,it's useless.
            bool isReadOnly = false;
            string varName = ((MemberExpression)exp.Body).Member.Name;

            // reflect to get variable names
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MemberInfo memberInfo = GetType().GetMember(varName, bindingFlags).FirstOrDefault();
            if (memberInfo != null)
            {
                MenuAttribute attr = memberInfo.GetCustomAttributes(false).FirstOrDefault(_ => _ is MenuAttribute) as MenuAttribute;
                if (attr != null)
                {
                    varName = isAddHeader ? attr.ToString() : attr.Name;
                }
            }

            // enum type
            if (typeof(T).BaseType == typeof(Enum))
            {
                return new CPluginVariable(varName, CreateEnumString(typeof(T)), Enum.GetName(typeof(T), exp.Compile()()), isReadOnly);
            }

            // other type
            return new CPluginVariable(varName, typeof(T), exp.Compile()(), isReadOnly);
        }

        #endregion
    }

    #region Template

    #region Menu Attribute

    /// <summary>
    /// Menu attribute for field. Get the variable by reflection according to the <see cref="Name"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class MenuAttribute : Attribute
    {
        /// <summary>
        /// The header of the variable in plugin setting tab.
        /// </summary>
        public readonly string Header;

        /// <summary>
        /// The name of the variable in plugin setting tab.it's unique value!!
        /// </summary>
        public readonly string Name;

        public override string ToString()
        {
            return Header + "|" + Name;
        }

        public MenuAttribute(string header, string name)
        {
            Header = header;
            Name = name;
        }
    }

    #endregion

    #region Procon Output

    /// <summary>
    /// <para><see cref="Trace"/> will be ignore when plugin compiled.</para>
    /// <para><see cref="Debug"/> need checked 'enable plugin debug' in PRoCon.</para>
    /// <para><see cref="Output"/> can be used anytime and anywhere in pcocon.</para>
    /// </summary>
    internal static class Output
    {
        public static bool AutoFlush { get; set; }

        public static TraceListenerCollection Listeners { get; private set; }

        static Output()
        {
            Listeners = typeof(TraceListenerCollection)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
                .Invoke(null) as TraceListenerCollection;
        }

        public static void Error(string format, params object[] args)
        {
            WriteLine(string.Format(format, args), TraceEventType.Error);
        }

        public static void Information(string format, params object[] args)
        {
            WriteLine(string.Format(format, args), TraceEventType.Information);
        }

        public static void Warning(string format, params object[] args)
        {
            WriteLine(string.Format(format, args), TraceEventType.Warning);
        }

        public static void Close()
        {
            foreach (TraceListener item in Listeners)
            {
                item.Flush();
                item.Close();
            }
        }

        public static void Flush()
        {
            foreach (TraceListener item in Listeners)
            {
                item.Flush();
            }
        }

        private static void WriteLine(string message, TraceEventType eventType)
        {
            foreach (TraceListener item in Listeners)
            {
                item.TraceEvent(new TraceEventCache(), string.Empty, eventType, 0, message);
                if (AutoFlush)
                {
                    item.Flush();
                }
            }
        }
        /// <summary>
        /// Write line message, support some escape character.
        /// <para>^0 Black</para>
        /// <para>^1 Maroon</para>
        /// <para>^2 Medium Sea Green</para>
        /// <para>^3 Dark Orange</para>
        /// <para>^4 Royal Blue</para>
        /// <para>^5 Cornflower Blue</para>
        /// <para>^6 Dark Violet</para>
        /// <para>^7 Deep Pink</para>
        /// <para>^8 Red</para>
        /// <para>^9 Grey</para>
        /// <para>^b Bold</para>
        /// <para>^n Normal</para>
        /// <para>^i Italicized</para>
        /// <para>^^ ^(Escape character)</para>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(string format, params object[] args)
        {
            foreach (TraceListener item in Listeners)
            {
                item.WriteLine(string.Format(format, args));
                if (AutoFlush)
                {
                    item.Flush();
                }
            }
        }
    }

    /// <summary>
    /// Procon trace listener.
    /// </summary>
    internal class PRoConTraceListener : TraceListener
    {
        private readonly string prefix;

        private readonly PRoConPluginAPI plugin;

        /// <summary>
        /// Construct, use pluginconsole output.
        /// </summary>
        /// <param name="pRoConPlugin">plugin instance</param>
        public PRoConTraceListener(PRoConPluginAPI pRoConPlugin) : this(pRoConPlugin, 0)
        { }

        /// <summary>
        /// Construct with output type.
        /// </summary>
        /// <param name="pRoConPlugin">plugin instance</param>
        /// <param name="outputType">0 pluginconsole;1 console;2 chat</param>
        public PRoConTraceListener(PRoConPluginAPI pRoConPlugin, int outputType)
        {
            plugin = pRoConPlugin;
            switch (outputType)
            {
                case 2:
                    prefix = "procon.protected.console.write";
                    break;

                case 1:
                    prefix = "procon.protected.chat.write";
                    break;

                case 0:
                default:
                    prefix = "procon.protected.pluginconsole.write";
                    break;
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }
            WriteLine(AddHeader(source, eventType, id) + message);
            WriteFooter(eventCache);
        }

        private string AddHeader(string source, TraceEventType eventType, int id)
        {
            string eventTypeName = eventType.ToString();
            switch (eventType)
            {
                case TraceEventType.Critical:
                    eventTypeName = "^7" + eventTypeName + ":^0";
                    break;
                case TraceEventType.Error:
                    eventTypeName = "^8" + eventTypeName + ":^0";
                    break;
                case TraceEventType.Warning:
                    eventTypeName = "^3" + eventTypeName + ":^0";
                    break;
                case TraceEventType.Information:
                    eventTypeName = "^4" + eventTypeName + ":^0";
                    break;
                case TraceEventType.Verbose:
                    eventTypeName = "^2" + eventTypeName + ":^0";
                    break;
                default:
                    eventTypeName = "^0" + eventTypeName + ":^0";
                    break;
            }
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} ", new object[]
            {
                string.IsNullOrEmpty(source) ? string.Empty : string.Format("[{0}] ",source),
                eventTypeName,
            });
        }

        private void WriteFooter(TraceEventCache eventCache)
        {
            if (eventCache == null)
                return;
            IndentLevel++;
            if (IsEnabled(TraceOptions.ProcessId))
            {
                WriteLine("ProcessId=" + eventCache.ProcessId);
            }
            if (IsEnabled(TraceOptions.LogicalOperationStack))
            {
                string stack = "LogicalOperationStack=";
                Stack logicalOperationStack = eventCache.LogicalOperationStack;
                bool flag = true;
                foreach (object obj in logicalOperationStack)
                {
                    if (!flag)
                    {
                        stack += ", ";
                    }
                    else
                    {
                        flag = false;
                    }
                    stack += obj.ToString();
                }
                WriteLine(stack);
            }
            if (IsEnabled(TraceOptions.ThreadId))
            {
                WriteLine("ThreadId=" + eventCache.ThreadId);
            }
            if (IsEnabled(TraceOptions.DateTime))
            {
                WriteLine("DateTime=" + eventCache.DateTime.ToString("o", CultureInfo.InvariantCulture));
            }
            if (IsEnabled(TraceOptions.Timestamp))
            {
                WriteLine("Timestamp=" + eventCache.Timestamp);
            }
            if (IsEnabled(TraceOptions.Callstack))
            {
                WriteLine("Callstack=" + eventCache.Callstack);
            }
            IndentLevel--;
        }

        private bool IsEnabled(TraceOptions opts)
        {
            return (opts & TraceOutputOptions) > TraceOptions.None;
        }

        public override void Write(string message)
        {
            plugin.ExecuteCommand(prefix, message);
        }

        public override void WriteLine(string message)
        {
            Write(string.Format("[{0}] {1}", plugin.ClassName, message));
        }
    }

    #endregion

    #endregion
}