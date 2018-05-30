using System.Collections.Generic;

public class HTTPResponse
{
    private string status;
    private string version;
    private string description;
    private List<Header> headers;
    private string body;

    public HTTPResponse(string data)
    {
        body = "";
        string[] lines = data.Split(new char[] { '\n' });
        if (lines.Length == 0) return;
        string[] l0 = lines[0].Split(new char[] { ' ' });
        if(l0.Length > 0) version = l0[0];
        if(l0.Length > 1) status = l0[1];
        if(l0.Length > 2) description = l0[2];

        int h = 1;
        headers = new List<Header>();

        while(h < lines.Length && lines[h] != "")
        {
            Header temp = new Header(lines[h]);
            if(temp.name != "content-length")
            {
                headers.Add(temp);
            }
            h++;
        }

        while(h < lines.Length)
        {
            body += lines[h++] + '\n';
        }
        headers.Add( new Header("content-length", body.Length.ToString()));
    }

    public HTTPResponse(string v, string s, string d, string h, string b)
    {
        version = v;
        status = s;
        description = d;

        string[] hl = h.Split(new char[] { '\n' });
        headers = new List<Header>();
        for(int i = 0; i < hl.Length; i++)
        {
            Header temp = new Header(hl[i]);
            if(temp.name != "content-length")
            {
                headers.Add(temp);
            }
        }
        body = b;
        headers.Add(new Header("content-length", body.Length.ToString()));
    }

    public HTTPResponse(int s, string b)
    {
        version = "HTTP/1.1";
        status = s.ToString();
        body = "";
        description = getDescription(s);
        headers = new List<Header>();
        headers.Add(new Header("server", "SamanaWS"));
        headers.Add(new Header("cache-control", "private"));
        headers.Add(new Header("connection", "close"));
        if(s >= 200 && s <= 299)
        {
            body = b;
            headers.Add(new Header("content-type", "application/json"));
        }
        else if(s >= 300 && s <= 399)
        {
            body = "<HTML><HEAD><meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">"
                + "<TITLE>" + status + " Moved</TITLE></HEAD><BODY>"
                + "<H1>" + status + " Moved </H1>"
                + "The document has moved "
                + "<A HREF=\"" + b + "\">here</A>.</BODY></HTML>";
            headers.Add(new Header("content-type", "text/html; charset=UTF-8"));
            headers.Add(new Header("location", b));
        }
        else if(s >= 400 && s <= 599)
        {
            if(b == null)
            {
                body = "<!DOCTYPE HTML PUBLIC \" -//IETF//DTD HTML 2.0//EN\">\r\n" +
                    "<html><head>\r\n" +
                    "<title>" + status + " Bad Request</title>\r\n" +
                    "</head><body>\r\n" +
                    "<h1>Bad Request</h1>\r\n" +
                    "<p>Your browser sent a request that this server could not understand.<br/>\r\n" +
                    "</p>\r\n<hr>\r\n" +
                    "</body></html>\r\n";
            } else
            {
                body = b;
            }
            headers.Add(new Header("content-type", "text/html; charset=UTF-8"));
        }
        headers.Add(new Header("content-length", body.Length));

    }

    public string getDescription(int s)
    {
        switch (s)
        {
            case 200: return "OK";
            case 201: return "Created";
            case 202: return "Accepted";
            case 203: return "Non - Authoritative Information";
            case 204: return "No Content";
            case 205: return "Reset Content";
            case 206: return "Partial Content";
            case 207: return "Multi - Status";
            case 208: return "Already Reported";
            case 226: return "IM Used";
            case 300: return "Multiple Choices";
            case 301: return "Moved Permanently";
            case 302: return "Found";
            case 303: return "See Other";
            case 304: return "Not Modified";
            case 305: return "Use Proxy";
            case 306: return "Switch Proxy";
            case 307: return "Temporary Redirect";
            case 308: return "Permanent Redirect";
            case 400: return "Bad Request";
            case 401: return "Unauthorized";
            case 402: return "Payment Required";
            case 403: return "Forbidden";
            case 404: return "Not Found";
            case 405: return "Method Not Allowed";
            case 406: return "Not Acceptable";
            case 407: return "Proxy Authentication Required";
            case 408: return "Request Timeout";
            case 409: return "Conflict";
            case 410: return "Gone";
            case 411: return "Length Required";
            case 412: return "Precondition Failed";
            case 413: return "Payload Too Large";
            case 414: return "URI Too Long";
            case 415: return "Unsupported Media Type";
            case 416: return "Range Not Satisfiable";
            case 417: return "Expectation Failed";
            case 418: return "I'm a teapot";
            case 421: return "Misdirected Request";
            case 422: return "Unprocessable Entity";
            case 423: return "Locked";
            case 424: return "Failed Dependency";
            case 426: return "Upgrade Required";
            case 428: return "Precondition Required";
            case 429: return "Too Many Requests";
            case 431: return "Request Header Fields Too Large";
            case 451: return "Unavailable For Legal Reasons";
            case 500: return "Internal Server Error";
            case 501: return "Not Implemented";
            case 502: return "Bad Gateway";
            case 503: return "Service Unavailable";
            case 504: return "Gateway Timeout";
            case 505: return "HTTP Version Not Supported";
            case 506: return "Variant Also Negotiates";
            case 507: return "Insufficient Storage";
            case 508: return "Loop Detected";
            case 510: return "Not Extended";
            case 511: return "Network Authentication Required";
            default: return "Unknown";
        }
    }
    

    public override string ToString()
    {
        string o = "";
        o += version + " ";
        o += status + " ";
        o += description + "\n";

        for(int i = 0; i < headers.Count; i++)
        {
            o += headers[i].ToString();
        }
        o += "\n";
        o += body;
        return o;
    }
}
