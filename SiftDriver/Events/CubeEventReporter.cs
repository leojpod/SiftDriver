using System;
using System.Collections.Generic;
using Sifteo;
using SiftDriver.Communication;

namespace SiftDriver.Events
{
	public class Tilt{
    public int _x;
    public int X{
      get{return _x;}
      set{
        if(IsValidTiltValue(value)){
          _x = value;
        }else{
          throw new ArgumentException("the given value is not a valid tilt value: "+value+" is not in [-1,1]");
        }
      }
    }

    public int _y;
    public int Y{
      get{return _y;}
      set{
        if(IsValidTiltValue(value)){
          _y = value;
        }else{
          throw new ArgumentException("the given value is not a valid tilt value: "+value+" is not in [-1,1]");
        }
      }
    }

    public int _z;
    public int Z{
      get{return _z;}
      set{
        if(IsValidTiltValue(value)){
          _z = value;
        }else{
          throw new ArgumentException("the given value is not a valid tilt value: "+value+" is not in [-1,1]");
        }
      }
    }

    public Tilt(int[] tilt){
      if(tilt.Length != 3){
        throw new ArgumentException("The tilt must contain 3 and only 3 elements and the given tilt contain "+tilt.Length+" elements","tilt");
      }else{
        X = tilt[0];
        Y = tilt[1];
        Z = tilt[2];
      }
    }

    public Tilt(int x, int y, int z)
      : this(new int[3] {x,y,z})
    {
    }
    public Tilt() : this(0,0,0){}

    private bool IsValidTiltValue(int aValue){
      return aValue <= 1 && aValue >= -1;
    }

		public Dictionary<string, object> ToParameters() {
			Dictionary<String, Object> parameters = new Dictionary<String, Object>();
      parameters.Add("x", X);
      parameters.Add("y", Y);
      parameters.Add("z", Z);
			return parameters;
		}

		public static Tilt NormalizeTilt(int[] tilt){
      if(tilt.Length != 3){
        throw new ArgumentException("The tilt must contain 3 and only 3 elements and the given tilt contain "+tilt.Length+" elements","tilt");
      }else{
        return NormalizeTilt(tilt[0], tilt[1], tilt[2]);
      }
    }

    public static Tilt NormalizeTilt(int x, int y, int z){
      Tilt tilt = new Tilt();
      switch(x){
      case 0:
        tilt.X = -1;
        break;
      case 1:
        tilt.X = 0;
        break;
      case 2:
        tilt.X = 1;
        break;
      default:
        tilt.X = 418; //using teapot HTTP code as unexpected tilt value...
        break;
      }

      switch(y){
      case 0:
        tilt.Y = 1;
        break;
      case 1:
        tilt.Y = 0;
        break;
      case 2:
        tilt.Y = -1;
        break;
      default:
        tilt.Y = 418; //using teapot HTTP code as unexpected tilt value...
        break;
      }

      switch(z){
      case 0:
        tilt.Z = -1;
        break;
      case 1:
        tilt.Z = 0;
        break;
      case 2:
        tilt.Z = 1;
        break;
      default:
        tilt.Z = 418; //using teapot HTTP code as unexpected tilt value...
        break;
      }

      return tilt;
    }
  }
  

  public class CubeEventReporter
  {
    private static List<CubeEventReporter> Reporters = new List<CubeEventReporter>();
    private JsonTcpCommunication _com;
    private Cube _c;
    public String CubeId{
      get{ return _c.UniqueId;}}

    public CubeEventReporter(JsonTcpCommunication com, Cube c){
      _com = com;
      _c = c;
      Reporters.Add(this);
      ReportAllEvents();
    }

    private void ReportAllEvents ()
    {
      //report all the possible event!

      _c.ButtonEvent += ButtonNotification;
      _c.FlipEvent += FlipNotification;
      _c.NeighborAddEvent += NeighborAddNotification;
      _c.NeighborRemoveEvent += NeighborRemoveNotification;
      _c.ShakeStartedEvent += ShakeStartedNofitication;
      _c.ShakeStoppedEvent += ShackStoppedNotification;
      _c.TiltEvent += TiltNotification;
    }

    private void TiltNotification (Cube c, int x, int y, int z)
    {
      if(!c.IsShaking){
        //same remark than for flipnotification
				Tilt tilt = Tilt.NormalizeTilt(x,y,z);
        this.NotifyEvent("tilt", c, tilt.ToParameters());
      }
    }

    private void ShackStoppedNotification (Cube c, int duration)
    {
      Dictionary<String, Object> parameters = new Dictionary<String, Object>();
      String msg = "shackingOver";
      parameters.Add("duration", duration);
      this.NotifyEvent(msg, c, parameters);
    }

    private void ShakeStartedNofitication (Cube c)
    {
      String msg = "shackingStarting";
      this.NotifyEvent(msg, c);
    }

    private void NeighborRemoveNotification (Cube c, Cube.Side side, Cube neighbor, Cube.Side neighborSide)
    {
      Dictionary<String, Object> parameters = new Dictionary<String, Object>();
      String msg = "neighborRemoved";
      parameters.Add("neighborId",neighbor.UniqueId);
      parameters.Add("cubeSide", side.ToString());
      parameters.Add("neighborSide" , neighborSide.ToString());
      this.NotifyEvent(msg, c, parameters);
    }

    private void NeighborAddNotification (Cube c, Cube.Side side, Cube neighbor, Cube.Side neighborSide)
    {
      Dictionary<String, Object> parameters = new Dictionary<String, Object>();
      String msg = "neighborAdded";
      parameters.Add("neighborId",neighbor.UniqueId);
      parameters.Add("cubeSide", side.ToString());
      parameters.Add("neighborSide" , neighborSide.ToString());
      this.NotifyEvent(msg, c, parameters);
    }

    private void FlipNotification (Cube c, bool newOrientationIsUp)
    {
      if(!c.IsShaking){//NOTE during the test I noticed that shaking a cube fire randomly some flip event...
        //Thus this test should prevent it
        String msg;
        if(newOrientationIsUp){
          msg = "flipedUp";
        }else{
          msg = "flipDown";
        }
        this.NotifyEvent(msg, c);
      }
    }

    private void ButtonNotification(Cube c , bool isPressed){
      String msg;
      if(isPressed){
        msg = "pressed";
      }else{
        msg = "released";
      }
      this.NotifyEvent(msg, c);
    }

    private void NotifyEvent(String str, Cube c){
      Dictionary<String, Object> msg = new Dictionary<String, Object>();
      msg.Add("event", str);
      msg.Add("devId", c.UniqueId);
      _com.SendEventMessage(msg);
    }
    private void NotifyEvent(String str, Cube c, Dictionary<String, Object> parameters){
      Dictionary<String, Object> msg = new Dictionary<String, Object>();
      msg.Add("event", str);
      msg.Add("devId", c.UniqueId);
      msg.Add("params", parameters);
      _com.SendEventMessage(msg);
    }

    public static bool ExistsReporter(String cubeId){
      foreach(CubeEventReporter r in Reporters){
        if(r.CubeId.Equals(cubeId)){
          return true;
        }
      }
      return false;
    }
  }
}

