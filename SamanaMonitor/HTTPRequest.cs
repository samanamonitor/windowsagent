using System;
using System.Collections.Generic;

class Header
{
    public string name;
    public string value;

    public Header(string data)
    {
        int separator = data.IndexOf(':');
        if (separator < 2) throw new Exception("Invalid Header");

        name = data.Substring(0, separator).ToLower();
        if (data.Length > separator + 2)
            value = data.Substring(separator + 2);
        else
            value = "";
    }

    public Header(string n, string v)
    {
        name = n;
        value = v;
    }

    public Header(string n, int v)
    {
        name = n;
        value = v.ToString();
    }

    public override string ToString()
    {
        string o;
        o = name + ": " + value + "\n";
        return o;
    }
}

public class HTTPRequest
{
    public string method;
    private string version;
    public string url;
    private List<Header> headers;
    private bool expect_body;
    public uint content_length;
    public string body;

    public HTTPRequest(string data)
    {
        string[] lines;
        expect_body = false;
        content_length = 0;
        char[] line_separator = { '\n' };

        lines = data.Split(line_separator);
        if (lines.Length < 2) throw new Exception("Invalid Request: \n" + data);

        string[] l0;
        l0 = lines[0].Split(new char[] { ' ' });
        if (l0.Length != 3) throw new Exception("Invalid Request: \n" + data);
        if(l0.Length >= 0) method = l0[0];
        if(l0.Length > 0) url = l0[1];
        if(l0.Length > 1) version = l0[2];
        if (method != "GET" && method != "POST") throw new Exception("Invalid Method: " + data);

        body = "";
        headers = new List<Header>();

        for (int i = 1; i < lines.Length; i++)
        {
            Header temp = new Header(lines[i]);
            headers.Add(temp);
            if(temp.name == "content-length")
            {
                content_length = Convert.ToUInt32(temp.value);
                if(content_length > 0) expect_body = true;
            }
        }
    }

    public bool has_body()
    {
        return expect_body;
    }

    public override string ToString()
    {
        string o;
        o = "Method: " + method + "\n";
        o += "Url: " + url + "\n";
        o += "Headers: \n";
        for(int i = 0; i < headers.Count; i++)
        {
            o += " " + headers[i].ToString();
        }
        if(has_body())
        {
            o += "Body: " + body + "\n";
        }
        return o;
    }
}
