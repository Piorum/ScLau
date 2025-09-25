using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{

    private class LatexStreamRewriter
    {
        private bool waitingForNext = false;
        private readonly StringBuilder sb = new();

        public string ProcessChunk(string token)
        {
            sb.Clear();

            foreach (var c in token)
                if (waitingForNext)
                {
                    switch (c)
                    {
                        case '(':
                        case ')':
                            sb.Append('$');
                            break;
                        case '[':
                        case ']':
                            sb.Append("$$");
                            break;
                        default:
                            sb.Append('\\').Append(c); // not math, output the backslash + current char
                            break;
                    }

                    waitingForNext = false;
                }
                else
                    if (c == '\\')
                    waitingForNext = true; // hold until next character
                else
                    sb.Append(c);

            return $"{sb}";
        }
    }
}
