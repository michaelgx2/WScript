using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WScriptLib
{
    public class WUScriptRunner
    {
        public List<ScrBranch> RuningScripts = new List<ScrBranch>();
        public List<ScrBranch> RuningProcesses = new List<ScrBranch>();
        public Dictionary<string, WSParam> Parameters = new Dictionary<string, WSParam>();

        public ScrBranch CurrentScript { get { return RuningScripts.Count > 0 ? RuningScripts[0] : null; } }
        public ScrBranch CurrentProcess { get { return RuningProcesses.Count > 0 ? RuningProcesses[0] : null; } }

        private Dictionary<string, DelWScriptCallback> _callbacks = new Dictionary<string, DelWScriptCallback>();
        private bool _paused = false;
        private DelGetDeltaTime _deltaTime;

        public WUScriptRunner(DelGetDeltaTime deltaTimeGetter)
        {
            PrepareReplacement();
            PreparePresetFunction();
            _deltaTime = deltaTimeGetter;
        }

        public void Update()
        {
            if (_paused) return;
            if (CurrentProcess != null)
            {
                RunBranch(CurrentProcess);
            }
            else if (CurrentScript != null)
            {
                RunBranch(CurrentScript);
            }
        }

        public void RunBranch(ScrBranch branch)
        {
            if (branch.NextLineTimer <= 0)
            {
                if (branch.CurrentLine >= branch.Script.Cmds.Count)
                {
                    (branch == CurrentScript ? RuningScripts : RuningProcesses).RemoveAt(0);
                    Update();
                    return;
                }

                WScriptCmd cmd = branch.Script.Cmds[branch.CurrentLine];
                if (_callbacks.ContainsKey(cmd.CommandName))
                {
                    branch.NextLineTimer = _callbacks[cmd.CommandName](cmd);
                    branch.CurrentLine++;
                    if (branch.NextLineTimer <= 0)
                    {
                        //如果这个脚本不阻塞进程， 那立刻继续执行下一条
                        Update();
                    }
                }
                else
                {
                    throw new Exception("Didn't find function '" + cmd.CommandName + "' in system. Check spells of this script: " + (CurrentScript == null ? "" : CurrentScript.Script.ScrName));
                }
            }
            else
            {
                branch.NextLineTimer -= _deltaTime?.Invoke() ?? 0;
            }
        }

        public void RegisterCmd(string cmd, DelWScriptCallback callback)
        {
            _callbacks.Add(cmd, callback);
        }

        public void RegisterReplacement(string origin, string replacer)
        {
            if (WScript.StringReplacement.ContainsKey(origin))
            {
                WScript.StringReplacement[origin] = replacer;
            }
            else
            {
                WScript.StringReplacement.Add(origin, replacer);
            }
        }

        public void RunScript(WScript script, WUScrRunType runType = WUScrRunType.Append)
        {
            if (runType == WUScrRunType.Override)
            {
                RuningScripts.Clear();
                RuningScripts.Add(new ScrBranch(script));
            }
            else if (runType == WUScrRunType.Insert)
            {
                RuningScripts.Insert(0, new ScrBranch(script));
            }
            else
            {
                RuningScripts.Add(new ScrBranch(script));
            }
        }

        public void RunProcess(string proName)
        {
            if (CurrentScript != null && CurrentScript.Script.Processes.ContainsKey(proName))
            {
                RuningProcesses.Insert(0, new ScrBranch(CurrentScript.Script.Processes[proName]));
            }
            else
            {
                throw new Exception(proName + " does not exist in script '" + (CurrentScript == null ? "" : CurrentScript.Script.ScrName) + "'");
            }
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        private void PreparePresetFunction()
        {
            RegisterCmd("runproc", (WScriptCmd line) =>
            {
                string processName = line.Parameters[0].ToString();
                RunProcess(processName);
                return 0;
            });
            RegisterCmd("wait", (WScriptCmd line) =>
            {
                float time = Convert.ToSingle(line.Parameters[0]);
                return time;
            });
            //dim("key","变量类型(num/str/bool)"[,初始值]);//声明一个变量,初始值可以设为[$变量名]
            RegisterCmd("dim", (WScriptCmd line) =>
            {
                string key = line.Parameters[0].ToString();
                if(key == "i") throw new Exception("[i] can not be dim as a parameter. It's a name used by script system.");
                if(Parameters.ContainsKey(key)) throw new Exception("Cannot dim parameters with same names.");
                string type = line.Parameters[1].ToString();
                object val = line.Parameters.Count > 2 ? GetParamVal(line.Parameters[2]) : null;
                switch (type)
                {
                    case "number":
                    case "num":
                        Parameters.Add(key, new WSParam("num", val!=null?Convert.ToSingle(val):0));
                        break;
                    case "string":
                    case "str":
                        Parameters.Add(key, new WSParam("str", val == null?"":val.ToString()));
                        break;
                    case "bool":
                        Parameters.Add(key, new WSParam("bool", val != null && Convert.ToBoolean(val)));
                        break;
                }
                return 0;
            });
            //set("key",修改方式(=|+|-|*|/|%),新值(字符串只支持=和+运算,bool值除了=以外的运算都做反转处理));//值可以设为[$变量名]
            RegisterCmd("set", line =>
            {
                string key = line.Parameters[0].ToString();
                string method = line.Parameters[1].ToString();
                object val = GetParamVal(line.Parameters[2]);
                if (Parameters.ContainsKey(key))
                {
                    var param = Parameters[key];
                    switch (param.Type)
                    {
                        case "num":
                            switch (method)
                            {
                                case "=":
                                    param.Value = Convert.ToSingle(val);
                                    break;
                                case "+":
                                    param.Value = Convert.ToSingle(param.Value) + Convert.ToSingle(val);
                                    break;
                                case "-":
                                    param.Value = Convert.ToSingle(param.Value) - Convert.ToSingle(val);
                                    break;
                                case "*":
                                    param.Value = Convert.ToSingle(param.Value) * Convert.ToSingle(val);
                                    break;
                                case "/":
                                    param.Value = Convert.ToSingle(param.Value) / Convert.ToSingle(val);
                                    break;
                                case "%":
                                    param.Value = Convert.ToSingle(param.Value) % Convert.ToSingle(val);
                                    break;
                            }
                            break;
                        case "str":
                            switch (method)
                            {
                                case "=":
                                    param.Value = val.ToString();
                                    break;
                                case "+":
                                    param.Value = param.Value.ToString() + val.ToString();
                                    break;
                            }
                            break;
                        case "bool":
                            switch (method)
                            {
                                case "=":
                                    param.Value = Convert.ToBoolean(val);
                                    break;
                                default:
                                    param.Value = !Convert.ToBoolean(val);
                                    break;
                            }
                            break;
                    }
                }
                else
                {
                    throw new Exception("Parameter [" + key + "] does not exist.");
                }
                return 0;
            });
            //loop(循环子过程,循环次数);
            RegisterCmd("loop", line =>
            {
                string pro = line.Parameters[0].ToString();
                int times = Convert.ToInt32(line.Parameters[1]);
                for (int i = 0; i < times; i++)
                {
                    RunProcess(pro);
                }

                return 0;
            });
            //if("key",比较值(可以设为[$变量名]),"比较运算(=/>/</>=/<=/!=)",true子过程,false子过程);//字符串和布尔值只支持=
            RegisterCmd("if", line =>
            {
                string key = line.Parameters[0].ToString();
                object val = GetParamVal(line.Parameters[1]);
                string method = line.Parameters[2].ToString();
                string trueDo = line.Parameters[3].ToString();
                string falseDo = line.Parameters[4].ToString();
                var param = Parameters[key];
                bool res = false;
                switch (param.Type)
                {
                    case "num":
                        switch (method)
                        {
                            case "=":
                                res = Math.Abs(Convert.ToSingle(val) - Convert.ToSingle(param.Value)) < 0.000001f;
                                break;
                            case ">":
                                res = Convert.ToSingle(param.Value) > Convert.ToSingle(val);
                                break;
                            case ">=":
                                res = Convert.ToSingle(param.Value) >= Convert.ToSingle(val);
                                break;
                            case "<":
                                res = Convert.ToSingle(param.Value) < Convert.ToSingle(val);
                                break;
                            case "<=":
                                res = Convert.ToSingle(param.Value) <= Convert.ToSingle(val);
                                break;
                        }
                        break;
                    case "str":
                        switch (method)
                        {
                            case "=":
                                res = val.ToString() == param.Value.ToString();
                                break;
                        }
                        break;
                    case "bool":
                        switch (method)
                        {
                            case "=":
                                res = Convert.ToBoolean(val) == Convert.ToBoolean(param.Value);
                                break;
                        }
                        break;
                }
                RunProcess(res?trueDo:falseDo);
                return 0;
            });
        }

        private object GetParamVal(object val)
        {
            string str = val.ToString();
            if (str.StartsWith("[$") && str.EndsWith("]"))
            {
                string key = str.Substring(2, str.Length - 3);
                return Parameters[key].Value;
            }
            else
            {
                return val;
            }
        }

        private void PrepareReplacement()
        {
            RegisterReplacement("&n;", "\n");
            RegisterReplacement("&q;", "\"");
        }

        public string ReplaceParams(string content)
        {
            StringBuilder sb = new StringBuilder(content);
            foreach (var par in Parameters)
            {
                switch (par.Value.Type)
                {
                    case "num":
                        sb.Replace("[$" + par.Key + "]", par.Value.Value.ToString())
                            .Replace("[$0" + par.Key + "]", Convert.ToSingle(par.Value.Value).ToString("0"))
                            .Replace("[$1" + par.Key + "]", Convert.ToSingle(par.Value.Value).ToString("0.0"))
                            .Replace("[$2" + par.Key + "]", Convert.ToSingle(par.Value.Value).ToString("0.00"))
                            .Replace("[$3" + par.Key + "]", Convert.ToSingle(par.Value.Value).ToString("0.000"));
                        break;
                    case "str":
                        sb.Replace("[$" + par.Key + "]", par.Value.Value.ToString());
                        break;
                    case "bool":
                        sb.Replace("[$" + par.Key + "]", Convert.ToBoolean(par.Value.Value)?"YES":"NO");
                        break;
                }
            }

            return sb.ToString();
        }
    }

    public class ScrBranch
    {
        public WScript Script;
        public int CurrentLine = 0;
        public float NextLineTimer = 0;

        public ScrBranch(WScript script)
        {
            Script = script;
            CurrentLine = 0;
        }
    }

    public delegate float DelWScriptCallback(WScriptCmd line);
    public delegate float DelGetDeltaTime();

    public enum WUScrRunType
    {
        /// <summary>
        /// Run at the end of the script flow
        /// </summary>
        Append,
        /// <summary>
        /// Clear all running/waiting scripts and run this one only
        /// </summary>
        Override,
        /// <summary>
        /// Pause running script and run this one first.
        /// </summary>
        Insert
    }

    public class WSParam
    {
        public string Type;
        public object Value;

        public WSParam(string type, object value)
        {
            Type = type;
            Value = value;
        }
    }
}
