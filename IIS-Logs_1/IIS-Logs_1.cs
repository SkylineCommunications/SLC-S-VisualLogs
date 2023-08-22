using System;
using System.Collections.Generic;
using System.IO;
using Skyline.DataMiner.Analytics.GenericInterface;

[GQIMetaData(Name = "IISAPILogs")]
public class MyDataSource : IGQIDataSource, IGQIInputArguments, IGQIOnInit
{
    private GQIStringArgument _file = new GQIStringArgument("File") { IsRequired = true };

    private string filePath = String.Empty;

    public GQIColumn[] GetColumns()
    {
        List<GQIColumn> columns = new List<GQIColumn>
      {
         new GQIDateTimeColumn("Date Time"),
         new GQIStringColumn("Source IP"),
         new GQIStringColumn("Method"),
         new GQIStringColumn("URI"),
         new GQIDoubleColumn("Port"),
         new GQIStringColumn("Client IP"),
         new GQIStringColumn("Referer"),
         new GQIDoubleColumn("Status Code"),
         new GQIDoubleColumn("Duration"),
         new GQIDateTimeColumn("End"),
      };
        return columns.ToArray();
    }

    public GQIArgument[] GetInputArguments()
    {
        return new GQIArgument[] { _file };
    }

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        var rows = new List<GQIRow>();

        try
        {
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            string fileContents;
            using (StreamReader reader = new StreamReader(fileStream))
            {
                fileContents = reader.ReadToEnd();
            }

            string[] filerows = fileContents.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string filerow in filerows)
            {
                string[] cells = filerow.Split(' ');
                if (cells.Length == 15)
                {
                    DateTime start = Convert.ToDateTime(cells[0] + " " + cells[1]).ToUniversalTime();

                    List<GQICell> GQIcells = new List<GQICell>();
                    GQIcells.Add(new GQICell() { Value = start });//Start
                    GQIcells.Add(new GQICell() { Value = cells[2] });//Source IP
                    GQIcells.Add(new GQICell() { Value = cells[3] });//method
                    GQIcells.Add(new GQICell() { Value = cells[4] });//URI
                    GQIcells.Add(new GQICell() { Value = Convert.ToDouble(cells[6]) });//Port
                    GQIcells.Add(new GQICell() { Value = cells[8] });//Client IP
                    GQIcells.Add(new GQICell() { Value = cells[10] });//Referer
                    GQIcells.Add(new GQICell() { Value = Convert.ToDouble(cells[11]) });//Status Code
                    GQIcells.Add(new GQICell() { Value = Convert.ToDouble(cells[14]), DisplayValue = cells[14] + " ms" });//Duration
                    GQIcells.Add(new GQICell() { Value = start.AddMilliseconds(Convert.ToDouble(cells[14])) });//End

                    rows.Add(new GQIRow(GQIcells.ToArray()));
                }
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
        filePath = args.GetArgumentValue(_file);

        return new OnArgumentsProcessedOutputArgs();
    }

    public OnInitOutputArgs OnInit(OnInitInputArgs args)
    {
        return new OnInitOutputArgs();
    }
}