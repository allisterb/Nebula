namespace Nebula.Verifiers.TLAPlus;

using System;
using System.Text;

using tla2sany;


using static Result;

public class SANY : Runtime
{
    public static Result<string> Parse(string file)
    {
        if (!File.Exists(file))
        {
            return Failure<string>($"The file {file} could not be found.");
        }
        util.SimpleFilenameToStream fts = new util.SimpleFilenameToStream(Directory.GetParent(file)?.FullName);
        var spec = new tla2sany.modanalyzer.SpecObj(Path.GetFullPath(file), fts);        
        var sb = new StringBuilder();        
        var stream = new StringBuilderOutputStream(sb);        
        try
        {            
            tla2sany.drivers.SANY.frontEndParse(spec, new java.io.PrintStream(stream), false);
            return Success<string>(sb.ToString());
        }
        catch (Exception ex)
        {
            return Failure<string>($"Could not parse {file}", ex);
        }
    }
}
