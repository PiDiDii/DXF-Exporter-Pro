using System.Globalization;
using System.Text;

namespace InventorDxfExporter.Core;

/// <summary>Pure DXF text processing; contains no Inventor/COM dependencies.</summary>
public sealed class DxfProcessor
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public DxfProcessResult Process(string inputPath, string outputPath, DxfProcessOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(options);
        if (!File.Exists(inputPath)) throw new FileNotFoundException("Không tìm thấy DXF đầu vào.", inputPath);
        var lines = File.ReadAllLines(inputPath, DxfEncoding()).ToList();
        int bends = 0;
        if (options.BendMode == "Chỉ đánh dấu 2 đầu") bends = ConvertBendLinesToEndMarks(lines, options);
        if (options.AddInfoText)
            throw new ArgumentException("Cần ExportItem để chèn TEXT. Dùng overload Process(inputPath, outputPath, options, item).", nameof(options));
        bool valid = ValidateDxfLines(lines);
        if (!valid) throw new InvalidDataException("DXF không hợp lệ sau khi xử lý.");
        WriteAtomically(outputPath, lines);
        return new DxfProcessResult(bends, 0, valid);
    }

    public DxfProcessResult Process(string inputPath, string outputPath, DxfProcessOptions options, ExportItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(options);
        if (!File.Exists(inputPath)) throw new FileNotFoundException("Không tìm thấy DXF đầu vào.", inputPath);
        var lines = File.ReadAllLines(inputPath, DxfEncoding()).ToList();
        int bends = options.BendMode == "Chỉ đánh dấu 2 đầu" ? ConvertBendLinesToEndMarks(lines, options) : 0;
        int texts = 0;
        if (options.AddInfoText) { AddInfoTextToDxf(lines, item, options.TextAlignment, options.TextHeight, options.TextLineGap, options.TextYOffset); texts = 4; }
        bool valid = ValidateDxfLines(lines);
        if (!valid) throw new InvalidDataException("DXF không hợp lệ sau khi xử lý.");
        WriteAtomically(outputPath, lines);
        return new DxfProcessResult(bends, texts, valid);
    }

    public bool Validate(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return File.Exists(filePath) && ValidateDxfLines(File.ReadAllLines(filePath, DxfEncoding()));
    }

    public static Encoding DxfEncoding() => Encoding.Latin1;

    public static bool ValidateDxfLines(IList<string> lines)
    {
        if (lines.Count < 8 || lines.Count % 2 != 0) return false;

        bool entitiesSection = false, hasEntity = false, eof = false;
        string? activeSection = null;
        for (int i = 0; i < lines.Count; i += 2)
        {
            if (!int.TryParse(lines[i].Trim(), NumberStyles.Integer, Invariant, out var groupCode)
                || groupCode is < 0 or > 1071)
                return false;

            var value = lines[i + 1].Trim();
            if (groupCode == 0 && value.Equals("SECTION", StringComparison.OrdinalIgnoreCase))
            {
                if (activeSection is not null || i + 3 >= lines.Count
                    || !int.TryParse(lines[i + 2].Trim(), NumberStyles.Integer, Invariant, out var sectionCode)
                    || sectionCode != 2 || string.IsNullOrWhiteSpace(lines[i + 3]))
                    return false;

                activeSection = lines[i + 3].Trim();
                if (activeSection.Equals("ENTITIES", StringComparison.OrdinalIgnoreCase))
                    entitiesSection = true;
            }
            else if (groupCode == 0 && value.Equals("ENDSEC", StringComparison.OrdinalIgnoreCase))
            {
                if (activeSection is null) return false;
                activeSection = null;
            }
            else if (groupCode == 0 && value.Equals("EOF", StringComparison.OrdinalIgnoreCase))
            {
                if (activeSection is not null || i != lines.Count - 2) return false;
                eof = true;
            }
            else if (groupCode == 0
                && activeSection?.Equals("ENTITIES", StringComparison.OrdinalIgnoreCase) == true)
            {
                hasEntity = true;
            }

            if (eof && i != lines.Count - 2) return false;
        }

        return activeSection is null && entitiesSection && hasEntity && eof;
    }

    public static int ConvertBendLinesToEndMarks(List<string> lines, DxfProcessOptions options)
    {
        ArgumentNullException.ThrowIfNull(lines); ArgumentNullException.ThrowIfNull(options);
        int entStart = -1, entEnd = -1;
        for (int i = 0; i < lines.Count - 1; i += 2)
        {
            string code = lines[i].Trim(), value = lines[i + 1].Trim().ToUpperInvariant();
            if (entStart < 0 && code == "2" && value == "ENTITIES") entStart = i + 2;
            else if (entStart >= 0 && code == "0" && value == "ENDSEC") { entEnd = i; break; }
        }
        if (entStart < 0 || entEnd < 0) throw new InvalidDataException("Không tìm thấy ranh giới ENTITIES/ENDSEC.");
        var entities = new List<EntityRange>(); EntityRange? current = null;
        for (int i = entStart; i < entEnd; i += 2)
        {
            if (lines[i].Trim() != "0") continue;
            if (current != null) current.End = i;
            current = new EntityRange { Start = i, Type = lines[i + 1].Trim().ToUpperInvariant() }; entities.Add(current);
        }
        if (current != null) current.End = entEnd;
        var groups = new List<BendGroup>();
        foreach (var ent in entities)
        {
            if (ent.Type != "LINE") continue;
            string layer = ""; double x1=0,y1=0,x2=0,y2=0; bool a=false,b=false,c=false,d=false;
            for (int j=ent.Start+2;j<ent.End-1;j+=2)
            {
                string code=lines[j].Trim(), val=lines[j+1].Trim();
                if(code=="8") layer=val;
                else if(code=="10") a=double.TryParse(val,NumberStyles.Float,Invariant,out x1);
                else if(code=="20") b=double.TryParse(val,NumberStyles.Float,Invariant,out y1);
                else if(code=="11") c=double.TryParse(val,NumberStyles.Float,Invariant,out x2);
                else if(code=="21") d=double.TryParse(val,NumberStyles.Float,Invariant,out y2);
            }
            string layerUpper=layer.ToUpperInvariant(); if(layerUpper!="IV_BEND_UP"&&layerUpper!="IV_BEND_DOWN"||!(a&&b&&c&&d)) continue;
            double dx=x2-x1,dy=y2-y1,len=Math.Sqrt(dx*dx+dy*dy); if(len<0.000001) continue;
            double ux=dx/len,uy=dy/len;
            if(ux < -0.000000001 || (Math.Abs(ux)<=0.000000001&&uy<0)){ux=-ux;uy=-uy;}
            double nx=-uy,ny=ux,dist=x1*nx+y1*ny,t1=x1*ux+y1*uy,t2=x2*ux+y2*uy,lo=Math.Min(t1,t2),hi=Math.Max(t1,t2);
            var g=groups.FirstOrDefault(q=>q.Layer==layerUpper&&Math.Abs(q.Ux*uy-q.Uy*ux)<0.0001&&Math.Abs(q.D-dist)<0.05);
            if(g==null)
            {
                g=new BendGroup{Layer=layerUpper,Ux=ux,Uy=uy,D=dist,TMin=lo,TMax=hi};
                for(int j=ent.Start+2;j<ent.End-1;j+=2){string code=lines[j].Trim();if(code!="10"&&code!="11"&&code!="20"&&code!="21"&&code!="30"&&code!="31"&&code!="5"&&code!="330"){g.Template.Add(lines[j]);g.Template.Add(lines[j+1]);}}
                groups.Add(g);
            }
            else {g.TMin=Math.Min(g.TMin,lo);g.TMax=Math.Max(g.TMax,hi);}
            ent.Removed=true;
        }
        if(groups.Count==0)return 0;
        var result=lines.Take(entStart).ToList();
        foreach(var ent in entities) if(!ent.Removed) for(int k=ent.Start;k<ent.End;k++) result.Add(lines[k]);
        foreach(var g in groups)
        {
            bool up=g.Layer=="IV_BEND_UP";double tick=up?options.BendUpLen:options.BendDownLen,inset=up?options.BendUpInset:options.BendDownInset,total=g.TMax-g.TMin;
            if(total>=2*(inset+tick)+1){AddBendLine(result,g,g.TMin+inset,g.TMin+inset+tick);AddBendLine(result,g,g.TMax-inset-tick,g.TMax-inset);}
            else if(total-2*inset>0.1) AddBendLine(result,g,g.TMin+inset,g.TMax-inset);
            else AddBendLine(result,g,g.TMin,g.TMax);
        }
        result.AddRange(lines.Skip(entEnd));
        if(!ValidateDxfLines(result))throw new InvalidDataException("DXF không hợp lệ sau khi xử lý đường chấn.");
        lines.Clear();lines.AddRange(result);return groups.Count;
    }

    private static void AddBendLine(List<string> dest,BendGroup g,double t1,double t2)
    {
        double xa=-g.Uy*g.D+g.Ux*t1,ya=g.Ux*g.D+g.Uy*t1,xb=-g.Uy*g.D+g.Ux*t2,yb=g.Ux*g.D+g.Uy*t2;
        dest.AddRange(new[]{"0","LINE"});dest.AddRange(g.Template);
        dest.AddRange(new[]{"10",xa.ToString("0.####",Invariant),"20",ya.ToString("0.####",Invariant),"30","0","11",xb.ToString("0.####",Invariant),"21",yb.ToString("0.####",Invariant),"31","0"});
    }

    public static void AddInfoTextToDxf(List<string> lines,ExportItem item,string textAlignment,double textHeight,double textLineGap,double textYOffset)
    {
        ArgumentNullException.ThrowIfNull(lines);ArgumentNullException.ThrowIfNull(item);
        int entityEnd=-1;double minX=0,maxX=0,minY=0,maxY=0;bool hasX=false,hasY=false,inEntities=false;
        for(int i=0;i<lines.Count-1;i+=2)
        {
            string codeText=lines[i].Trim(),value=lines[i+1].Trim();if(!int.TryParse(codeText,out int code))continue;
            if(code==2&&value.ToUpperInvariant()=="ENTITIES")inEntities=true;
            else if(inEntities&&code==0&&value.ToUpperInvariant()=="ENDSEC"){entityEnd=i;break;}
            else if(inEntities&&(code==10||code==11||code==13||code==14)&&double.TryParse(value,NumberStyles.Float,Invariant,out double x)){if(!hasX){minX=maxX=x;hasX=true;}else{minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}}
            else if(inEntities&&(code==20||code==21||code==23||code==24)&&double.TryParse(value,NumberStyles.Float,Invariant,out double y)){if(!hasY){minY=maxY=y;hasY=true;}else{minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}}
        }
        if(entityEnd<0)throw new InvalidDataException("TEXT - Không tìm thấy ranh giới ENTITIES/ENDSEC.");
        double cx=(minX+maxX)/2,h=Math.Abs(textHeight);if(h<0.1)h=10;double gap=Math.Abs(textLineGap);if(gap<h*1.2)gap=h*1.2;double baseY=minY+textYOffset;
        int before=CountDxfEntities(lines);
        if(!EnsureInfoLayerAndStyle(lines))throw new InvalidDataException("TEXT - Không tìm thấy TABLES/LAYER/STYLE phù hợp để tạo layer/style thử nghiệm.");
        if(!RefreshMetrics(lines,out entityEnd,out minX,out maxX,out minY,out maxY))throw new InvalidDataException("TEXT - Không quét lại được ENTITIES sau khi cập nhật TABLES.");
        string[] info={"DO DAY: "+item.Thickness.Replace("T",""),"SO LUONG: "+item.Quantity.ToString(),"VAT LIEU: "+CleanTextForDxf(item.Material),"TEN CHI TIET: "+CleanTextForDxf(item.DetailName)};
        double startY=baseY+(info.Length-1)*gap;var insert=new List<string>();
        foreach(int n in Enumerable.Range(0,info.Length))
        {
            string text=info[n];double width=h*0.6*text.Length;double x;
            if(textAlignment=="Căn giữa"&&width<=maxX-minX){x=cx-width/2;if(x<minX)x=minX;if(x+width>maxX)x=maxX-width;}else x=minX;
            double y=startY-n*gap;
            insert.AddRange(new[]{"0","TEXT","8","DXF_INFO","10",x.ToString("0.###",Invariant),"20",y.ToString("0.###",Invariant),"30","0","40",h.ToString("0.###",Invariant),"1",text,"7","DXF_INFO_STYLE"});
        }
        lines.InsertRange(entityEnd,insert);int after=CountDxfEntities(lines);
        if(after!=before+info.Length)throw new InvalidDataException($"TEXT - Số entity thay đổi bất thường trước/sau khi chèn: {before} -> {after}");
        if(!ValidateDxfLines(lines))throw new InvalidDataException("TEXT - DXF không hợp lệ sau khi chèn.");
    }

    public static bool EnsureInfoLayerAndStyle(List<string> lines)
    {
        int layerTable=-1,styleTable=-1;
        for(int i=0;i<lines.Count-3;i+=2)if(lines[i].Trim()=="0"&&lines[i+1].Trim().ToUpperInvariant()=="TABLE") {if(lines[i+3].Trim().ToUpperInvariant()=="LAYER")layerTable=i;if(lines[i+3].Trim().ToUpperInvariant()=="STYLE")styleTable=i;}
        if(layerTable<0||styleTable<0)return false;
        int layerEnd=FindEndTab(lines,layerTable),styleEnd=FindEndTab(lines,styleTable);if(layerEnd<0||styleEnd<0)return false;
        bool hasLayer=HasTableName(lines,layerTable,layerEnd,"DXF_INFO"),hasStyle=HasTableName(lines,styleTable,styleEnd,"DXF_INFO_STYLE");
        if(!hasStyle){lines.InsertRange(styleEnd,new[]{"0","STYLE","2","DXF_INFO_STYLE","70","0","40","0","41","1","50","0","71","0","42","10","3","txt.shx","4",""});}
        if(!hasLayer){lines.InsertRange(layerEnd,new[]{"0","LAYER","2","DXF_INFO","70","0","62","7","6","CONTINUOUS"});}
        return true;
    }
    private static int FindEndTab(List<string> a,int start){for(int i=start;i<a.Count-1;i+=2)if(a[i].Trim()=="0"&&a[i+1].Trim().ToUpperInvariant()=="ENDTAB")return i;return -1;}
    private static bool HasTableName(List<string>a,int start,int end,string name){for(int i=start;i<end;i+=2)if(a[i].Trim()=="2"&&i+1<a.Count&&a[i+1].Trim().ToUpperInvariant()==name)return true;return false;}
    private static bool RefreshMetrics(List<string>a,out int end,out double minX,out double maxX,out double minY,out double maxY)
    {
        end=-1;minX=maxX=minY=maxY=0;bool xFound=false,yFound=false,inEnt=false;
        for(int i=0;i<a.Count-1;i+=2){string c=a[i].Trim(),v=a[i+1].Trim();if(!int.TryParse(c,out int code))continue;if(code==2&&v.ToUpperInvariant()=="ENTITIES")inEnt=true;else if(inEnt&&code==0&&v.ToUpperInvariant()=="ENDSEC"){end=i;break;}else if(inEnt&&code==10&&double.TryParse(v,NumberStyles.Float,Invariant,out double x)){if(!xFound){minX=maxX=x;xFound=true;}else{minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);}}else if(inEnt&&code==20&&double.TryParse(v,NumberStyles.Float,Invariant,out double y)){if(!yFound){minY=maxY=y;yFound=true;}else{minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}}}
        return end>=0;
    }
    public static int CountDxfEntities(IList<string> lines){bool inEntities=false;int count=0;for(int i=0;i<lines.Count-1;i+=2){string c=lines[i].Trim(),v=lines[i+1].Trim().ToUpperInvariant();if(c=="2"&&v=="ENTITIES")inEntities=true;else if(inEntities&&c=="0"&&v=="ENDSEC")break;else if(inEntities&&c=="0")count++;}return count;}
    public static string CleanTextForDxf(string? value)
    {
        if(string.IsNullOrEmpty(value))return "";string t=value.Replace("\r"," ").Replace("\n"," ").Replace('|',' ').Replace('đ','d').Replace('Đ','D');
        string decomposed=t.Normalize(NormalizationForm.FormD);var sb=new StringBuilder();foreach(char ch in decomposed)if(CharUnicodeInfo.GetUnicodeCategory(ch)!=UnicodeCategory.NonSpacingMark)sb.Append(ch);
        string output=sb.ToString().Normalize(NormalizationForm.FormC);var ascii=new StringBuilder();foreach(char ch in output)ascii.Append(ch>=32&&ch<=126?ch:'?');return ascii.ToString();
    }
    private static void WriteAtomically(string path,List<string> lines){string full=Path.GetFullPath(path),dir=Path.GetDirectoryName(full)!;Directory.CreateDirectory(dir);string temp=full+".tmp_"+Guid.NewGuid().ToString("N");try{File.WriteAllLines(temp,lines,DxfEncoding());File.Copy(temp,full,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
    private sealed class EntityRange{public int Start;public int End;public string Type="";public bool Removed;}
    private sealed class BendGroup{public string Layer="";public double Ux,Uy,D,TMin,TMax;public List<string> Template{get;}=new();}
}

public sealed record DxfProcessOptions(string DxfOptions, string BendMode, double BendUpLen, double BendUpInset,
    double BendDownLen, double BendDownInset, bool AddInfoText, string TextAlignment, double TextHeight,
    double TextLineGap, double TextYOffset);

public sealed record DxfProcessResult(int BendLinesProcessed, int TextEntitiesAdded, bool Valid);
