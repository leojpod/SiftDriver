using System;
using SiftDriver;
using Sifteo;

namespace SiftDriver.Utils
{
  public class ImageDisplayer
  {
    public static void DisplayPicture(Cube c, JsonPicture p){
      p.RenderOnCube(c);
    }
  }
  
  public class JsonPicture
  {
    /* JSON representation of a picture in our case:
     * {picutreBlocks:
     *    [ { color:{r:int, g:int, b:int},
     *        blocks:[
     *                 { x:int y:int w:int h:int }
     *                 ...
     *               ]
     *      },
     *      { color: ... }
     *    ] }
     */
    public JsonColorBlocks[] pictureBlocks;

    public JsonColorBlocks this[int idx] { get{return pictureBlocks[idx]; } }

    public void RenderOnCube(Cube c){
      foreach(JsonColorBlocks cBlocks in pictureBlocks){
        cBlocks.RenderOnCube(c);
      }
    }
  }

  public class JsonColorBlocks
  {
    // a color block represent only the {color : ... [ ]} part of a JsonPicture
    public JsonColor color;
    public JsonSimpleBlock[] blocks;

    public void RenderOnCube(Cube c){
      foreach(JsonSimpleBlock block in this.blocks){
        block.PrintColorOnCube(c, color.GetSifteoColor());
      }
    }
  }
  public class JsonColor
  {
    public int r,g,b;

    public override bool Equals (object obj)
    {
      JsonColor that = obj as JsonColor;
      return this.Equals(that);
    }

    public bool Equals (JsonColor that){
      if(that == null){
        return false;
      }//else
      return (this.r == that.r) && (this.g == that.g) && (this.b == that.b);
    }

    public override int GetHashCode ()
    {
      return r*(0x10000) + g*0x100 + b;
    }

    public Sifteo.Color GetSifteoColor(){
      return new Sifteo.Color(r, g, b);
    }
  }
  public class JsonSimpleBlock
  {
    // represent the {x:int, y:int, ...}
    public int x,y;
    public int w,h;

    public void PrintColorOnCube(Cube c, Sifteo.Color color){
      c.FillRect(color, x, y, w, h);
    }
  }
}
