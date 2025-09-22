using System.Text;
using ChatBackend.Models;

namespace ChatBackend.Builders;

public class GptOssPromptBuilder
{
    private string _systemMessage = "";
    private string _developerInstructions = "";
    private GptOssReasoningLevel _reasoningLevel = GptOssReasoningLevel.Low;

    public GptOssPromptBuilder WithSystemMessage(string systemMessage)
    {
        _systemMessage = systemMessage;
        return this;
    }

    public GptOssPromptBuilder WithDeveloperInstructions(string developerInstructions)
    {
        _developerInstructions = developerInstructions;
        return this;
    }

    public GptOssPromptBuilder WithReasoningLevel(GptOssReasoningLevel reasoningLevel)
    {
        _reasoningLevel = reasoningLevel;
        return this;
    }

    //Add appending tools (C# methods)
    //Ex tool channle restrain: (Inject at the end of the system message) "\nCalls to these tools must go to the commentary channel: 'functions'."
    //Ex tool string: (Inject directly after instructions in developer channel) "# Tools\n\n## functions\n\nnamespace functions {\n\n// Gets the location of the user.\ntype get_location = () => any;\n\n// Gets the current weather in the provided location.\ntype get_current_weather = (_: {\n// The city and state, e.g. San Francisco, CA\nlocation: string,\nformat?: \"celsius\" | \"fahrenheit\", // default: celsius\n}) => any;\n\n// Gets the current weather in the provided list of locations.\ntype get_multiple_weathers = (_: {\n// List of city and state, e.g. [\"San Francisco, CA\", \"New York, NY\"]\nlocations: string[],\nformat?: \"celsius\" | \"fahrenheit\", // default: celsius\n}) => any;\n\n} // namespace functions"

    public override string ToString()
    {
        GptOssChatBuilder gocb = new();

        gocb.Append(new(GptOssRole.System, GptOssChannel.None, $"{_systemMessage}\nKnowledge cutoff: 2024-06\nCurrent date: {DateTime.Now:yyyy-MM-dd}\n\nReasoning: {_reasoningLevel.ToString().ToLower()}\n\n# Valid channels: analysis, commentary, final. Channel must be included for every message."));
        gocb.Append(new(GptOssRole.Developer, GptOssChannel.None, $"# Instructions\n\n{_developerInstructions}\n\n"));


        return gocb.ToString();
    }

    public void Clear()
    {
        _systemMessage = "";
        _developerInstructions = "";
        _reasoningLevel = GptOssReasoningLevel.Low;
    }

}
