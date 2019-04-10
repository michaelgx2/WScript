# WScript
A very light weight script system. Mainly for Unity. But also usable in any C# projects.

# Samples
```C#
//Notice: following code samples are not supposed to run in a same method. Judge the place where codes should run by yourself.
//To read a script file.
WScript script = new WScript(File.ReadAllText("C:\\scr.txt"));
//To create a new ScriptRunner with deltaTime always been 0.1
WUScriptRunner runner = new WUScriptRunner(()=>0.1f);
//Run a script.
runner.Run(script);
```

For more information about how to use WScriptSystem, check the project called WScriptTest.
