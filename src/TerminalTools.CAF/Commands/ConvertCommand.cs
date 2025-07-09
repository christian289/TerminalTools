namespace TerminalTools.CAF.Commands;

public sealed class ConvertCommand(ILogger<ConvertCommand> logger)
{
    [Command("jy")]
    public void JsonToYamlConvertAsync(string jsonString)
    {
    }

    [Command("yj")]
    public void YamlToJsonConvertAsync(string yamlString)
    {

    }


}