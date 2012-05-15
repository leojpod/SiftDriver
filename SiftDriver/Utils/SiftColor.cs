using System;
using System.Collections.Generic;
using Cairo;
using Sifteo;

namespace SiftDriver.Utils
{
  public struct SiftColor
  {
    public int r, g, b;

    public Cairo.Color ToCairo(){
      return new Cairo.Color((r + 0.0) / 255.0,
                             (g + 0.0) / 255.0,
                             (b + 0.0) / 255.0);
    }
    public Sifteo.Color ToSifteo(){
      return new Sifteo.Color(r, g, b);
    }

    public SiftColor(Dictionary<string,object> colors): this(JsonProtocolHelper.AssertTypeInDic<int>(colors,"r"),
          JsonProtocolHelper.AssertTypeInDic<int>(colors,"g"),
          JsonProtocolHelper.AssertTypeInDic<int>(colors,"b")
          ){}

    public SiftColor(Cairo.Color color): this((int)color.R*255,
                                              (int)color.G*255,
                                              (int)color.B*255){}
    public SiftColor(byte c){
      r = (0xff0000 & c) >> 16;
      g = (0x00ff00 & c) >> 8;
      b = (0x0000ff);
    }

    public SiftColor(int cr, int cg, int cb){
      r = cr; g = cg; b = cb;
    }

    public SiftColor(Sifteo.Color c){
      //TODO let to be done
      r = 0; g = 0; b = 0;
    }

  }
}

