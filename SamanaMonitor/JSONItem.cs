using System.Collections.Generic;
using System.Text;
using System;

public class JSONItem
{
    public long ticks;
    public int id;

    public virtual string json() { ticks = 0;  return "{}"; }
    protected string escape(string s)
    {
        if (s == null || s.Length == 0)
        {
            return "\"\"";
        }

        char c = '\0';
        int len = s.Length;
        StringBuilder sb = new StringBuilder(len + 10);
        sb.Append('"');
        for (int i = 0; i < s.Length; i++)
        {
            c = s[i];
            switch (c)
            {
                case '\\':
                case '"':
                case '/':
                    sb.Append('\\');
                    sb.Append(c);
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                default:
                    if (c < ' ')
                    {
                        sb.Append("\\u" + Convert.ToByte(c).ToString("X4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}

public class JSONItemList : List<JSONItem>
{
    public long ticks;
    public string json()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"ticks\": " + ticks + ", ");
        sb.Append("\"data\": [");
        bool first = true;
        foreach (var i in this)
        {
            if (first) first = false;
            else sb.Append(',');
            sb.Append(i.json());
        }
        sb.Append(']');
        sb.Append('}');
        return sb.ToString();
    }
}
