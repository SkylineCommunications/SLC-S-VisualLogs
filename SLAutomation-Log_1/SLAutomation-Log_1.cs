using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Skyline.DataMiner.Analytics.GenericInterface;

[GQIMetaData(Name = "SLAutomationLogs")]
public class MyDataSource : IGQIDataSource, IGQIOnInit
{
    public GQIColumn[] GetColumns()
    {
        List<GQIColumn> columns = new List<GQIColumn>
      {
         new GQIDateTimeColumn("Start"),
         new GQIDateTimeColumn("End"),
         new GQIStringColumn("User"),
         new GQIStringColumn("Script"),
         new GQIDoubleColumn("Duration"),
      };
        return columns.ToArray();
    }

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        Dictionary<int, ScriptExecution> scriptExecutions = new Dictionary<int, ScriptExecution>();

        var rows = new List<GQIRow>();

        try
        {
            FileStream fileStream = new FileStream(@"C:\Skyline DataMiner\Logging\SLAutomation.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            string fileContents;
            using (StreamReader reader = new StreamReader(fileStream))
            {
                fileContents = reader.ReadToEnd();
            }

            string[] filerows = fileContents.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string filerow in filerows)
            {
                ParseLogMessage(filerow, scriptExecutions);
            }

            foreach (var scriptExecution in scriptExecutions.Values)
            {
                List<GQICell> GQIcells = new List<GQICell>();
                GQIcells.Add(new GQICell() { Value = scriptExecution.Start.ToUniversalTime() });
                GQIcells.Add(new GQICell() { Value = scriptExecution.End.ToUniversalTime() });
                GQIcells.Add(new GQICell() { Value = scriptExecution.User });
                GQIcells.Add(new GQICell() { Value = scriptExecution.Name });
                GQIcells.Add(new GQICell() { Value = scriptExecution.Duration *1000 , DisplayValue = (scriptExecution.Duration * 1000) + " ms" });

                rows.Add(new GQIRow(GQIcells.ToArray()));
            }
        }
        catch (Exception)
        {
            // fail gracefully
        }

        return new GQIPage(rows.ToArray())
        {
            HasNextPage = false,
        };
    }

    public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
    {
        return new OnArgumentsProcessedOutputArgs();
    }

    public OnInitOutputArgs OnInit(OnInitInputArgs args)
    {
        return new OnInitOutputArgs();
    }

    static void ParseLogMessage(string logMessage, Dictionary<int, ScriptExecution> scriptExecutions)
    {
        string pattern = @"(\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}\.\d{3}).*?\[(.*?)\].*?Finished executing script: '(.*?)' \(ID: (\d+)\).*?Execution took (\d+\.\d+)s";
        Match match = Regex.Match(logMessage, pattern);

        if (match.Success)
        {
            DateTime timestamp = DateTime.ParseExact(match.Groups[1].Value, "yyyy/MM/dd HH:mm:ss.fff", null);
            string user = match.Groups[2].Value;
            string scriptName = match.Groups[3].Value;
            int scriptId = Convert.ToInt32(match.Groups[4].Value);
            double durationInSeconds = double.Parse(match.Groups[5].Value);
            TimeSpan duration = TimeSpan.FromSeconds(durationInSeconds);

            try
            {
                scriptExecutions.Add(scriptId, new ScriptExecution { Start = timestamp.Subtract(duration), End = timestamp, Duration = durationInSeconds, Name = scriptName, User = user });
            }
            catch (Exception)
            {
                // the same key was already in the dictionary?
            }
        }
    }

    public class ScriptExecution
    {
        public string Name { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public double Duration { get; set; }

        public string User { get; set; }

    }
}