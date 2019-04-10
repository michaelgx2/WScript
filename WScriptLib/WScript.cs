using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WScriptLib
{
    public class WScript
    {
        public string ScrName = "";
        public List<WScriptCmd> Cmds = new List<WScriptCmd>();
        public Dictionary<string, WScript> Processes = new Dictionary<string, WScript>();
        public static Dictionary<string, string> StringReplacement = new Dictionary<string, string>(); //全局设置，需要被替换的字符串转义符

        public WScript() { }

        public WScript(string script, string name = "")
        {
            ScrName = name;
            ParseScript(script);
        }

        //解析分拆脚本文件
        private void ParseScript(string script)
        {
            bool isComment = false;
            bool isString = false;
            bool isBlock = false;
            int currentLine = 0;

            string buff = "";
            WScriptCmd temp = new WScriptCmd();
            char last = char.MinValue;
            int blockQuote = 0;//读取子代码块时里面包含多少大括号组
            foreach (var c in script)
            {
                if (isString)
                {
                    //如果正在读取字符串
                    if (c == '"')
                    {
                        //字符串结尾。
                        if (isBlock)
                        {
                            //如果这是个子代码块
                            buff += c;
                        }
                        else
                        {
                            //替换字符串
                            foreach (var replace in StringReplacement)
                            {
                                buff = buff.Replace(replace.Key, replace.Value);
                            }
                            temp.Parameters.Add(buff); //加入字符串参数
                            buff = "";
                        }
                        isString = false;
                    }
                    else
                    {
                        buff += c;
                    }
                }
                else
                {
                    if (!isComment)
                    {
                        //如果正在读取代码而非注释
                        if (c == '/')
                        {
                            if (last == '/')
                            {
                                isComment = true;
                            }
                            else
                            {
                                //应该不需要处理
                            }
                        }
                        else if (c == '(')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块，原样保留
                                buff += c;
                            }
                            else
                            {
                                //函数名称结束，函数参数开始
                                temp.CommandName = buff.Trim();
                                buff = "";
                            }
                        }
                        else if (c == ',')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块 原样保留
                                buff += c;
                            }
                            else
                            {
                                //一个参数的结尾
                                float tempf = 0;
                                long templ = 0;
                                int tempi = 0;
                                bool tempb = false;
                                if (int.TryParse(buff, out tempi))
                                {
                                    temp.Parameters.Add(tempi);//Add int if can be converted to int
                                }
                                else if (long.TryParse(buff, out templ))
                                {
                                    temp.Parameters.Add(templ);//Add long if can be converted to long
                                }
                                else if (float.TryParse(buff, out tempf))
                                {
                                    temp.Parameters.Add(tempf);//Add float if can be converted to float
                                }
                                else if (bool.TryParse(buff, out tempb))
                                {
                                    temp.Parameters.Add(tempb);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(buff.Trim()))
                                    {
                                        temp.Parameters.Add("&" + buff.Trim()); //Add as process name
                                    }
                                    else
                                    {
                                        //参数字符串为空，舍弃这个参数，因为如果这是个空字符串参数，上面已经处理完了
                                    }
                                }

                                buff = "";
                            }
                        }
                        else if (c == ')')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块 原样保留
                                buff += c;
                            }
                            else
                            {
                                //最后一个参数的结尾
                                float tempf = 0;
                                long templ = 0;
                                int tempi = 0;
                                bool tempb = false;
                                if (int.TryParse(buff, out tempi))
                                {
                                    temp.Parameters.Add(tempi);//Add int if can be converted to int
                                }
                                else if (long.TryParse(buff, out templ))
                                {
                                    temp.Parameters.Add(templ);//Add long if can be converted to long
                                }
                                else if (float.TryParse(buff, out tempf))
                                {
                                    temp.Parameters.Add(tempf);//Add float if can be converted to float
                                }
                                else if (bool.TryParse(buff, out tempb))
                                {
                                    temp.Parameters.Add(tempb);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(buff.Trim()))
                                    {
                                        temp.Parameters.Add("&" + buff.Trim()); //Add as process name
                                    }
                                    else
                                    {
                                        //参数字符串为空，舍弃这个参数，因为如果这是个空字符串参数，上面已经处理完了
                                    }
                                }

                                buff = "";
                            }
                        }
                        else if (c == ';')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块 原样保留
                                buff += c;
                            }
                            else
                            {
                                //一行代码的结尾
                                Cmds.Add(temp);

                                //重置全部标志位和临时变量
                                isComment = false;
                                isString = false;
                                isBlock = false;
                                temp = new WScriptCmd();
                                buff = "";
                                last = char.MinValue;
                                blockQuote = 0;
                            }
                        }
                        else if (c == '"')
                        {
                            //字符串开头
                            isString = true;
                            if (isBlock)
                            {
                                //如果是子代码块中的字符串，原样保留
                                buff += c;
                            }
                            else
                            {
                                buff = "";
                            }
                        }
                        else if (c == '{')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块，原样保留并且计数
                                buff += c;
                                blockQuote++;
                            }
                            else
                            {
                                //开始一个子代码块
                                isBlock = true;
                                //临时记录子代码块函数名称
                                temp.CommandName = buff.Trim();
                                buff = "";
                            }
                        }
                        else if (c == '}')
                        {
                            if (!isBlock)
                            {
                                throw new FormatException("Unexpected '}'");
                            }
                            else
                            {
                                if (blockQuote > 0)
                                {
                                    //依然在子代码块中，原样保留
                                    buff += c;
                                    blockQuote--;
                                }
                                else
                                {
                                    //子代码块的结尾，处理子代码块
                                    if (Processes.ContainsKey(temp.CommandName))
                                    {
                                        throw new FormatException("The process name '" + temp.CommandName +
                                                                  "' has existed in this script block. Line: " + currentLine);
                                    }
                                    else
                                    {
                                        Processes.Add("&" + temp.CommandName, new WScript(buff, temp.CommandName));
                                    }

                                    //重置全部标志位和临时变量
                                    isComment = false;
                                    isString = false;
                                    isBlock = false;
                                    temp = new WScriptCmd();
                                    buff = "";
                                    last = char.MinValue;
                                    blockQuote = 0;
                                }
                            }
                        }
                        else if (c == '\r')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块中的字符串，原样保留
                                buff += c;
                            }
                        }
                        else if (c == '\n')
                        {
                            if (isBlock)
                            {
                                //如果是子代码块中的字符串，原样保留
                                buff += c;
                            }
                            currentLine++;
                        }
                        else
                        {
                            buff += c;
                        }
                    }
                    else
                    {
                        //如果正在读取注释 则换行注释就结束
                        if (c == '\n')
                        {
                            isComment = false;
                            currentLine++;
                        }
                    }
                }

                last = c;
            }
        }
    }

    public class WScriptCmd
    {
        public string CommandName = "";
        public List<object> Parameters = new List<object>();

        public WScriptCmd() { }

        public WScriptCmd(string cmd, params object[] paras)
        {
            CommandName = cmd;
            foreach (var para in paras)
            {
                Parameters.Add(para);
            }
        }
    }

    public delegate string DelReplacement();
}
