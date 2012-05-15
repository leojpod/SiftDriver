using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

using SiftDriver.Communication;
using SiftDriver.Events;
using SiftDriver.Utils;

using Sifteo;

using JsonFx.Json;


namespace SiftDriver.Communication.Protocols
{
  public class GeneralCommunicationProtocol: JsonTcpCommunication
  {
    private JsonReaderThread _readerThread;
    private Thread _containingThread;
    private AppManager _appMgr = AppManagerAccess.Instance;

    public GeneralCommunicationProtocol(TcpClient socket) : base(socket)
    {
      _readerThread = new JsonReaderThread(this);
      _containingThread = new Thread(new ThreadStart(_readerThread.ReadingLoop));
      //then setup the callbacks and run it!
      _readerThread.IncomingMessage += delegate(Dictionary<string,object> msg){
        if(msg == null){
          Log.Info("the comminucation with the API is over or lost, we should do something about it");
          AppManagerAccess.Instance.TurnOffDriver();
        }
        if(msg.ContainsKey("flow")
           && msg["flow"].GetType().Equals(typeof(String))
           && msg.ContainsKey("msg")
           && msg["msg"].GetType().Equals(typeof(Dictionary<String,Object>))
           ){
          switch((string) msg["flow"]){
          case "ctrl":
            onCtrlMessage((Dictionary<string, object>) msg["msg"]);
            break;
          case "event":
            onEventMessage((Dictionary<string, object>) msg["msg"]);
            break;
          default:
            Log.Info("this message is invalid: <<<"+ new JsonWriter().Write(msg)+">>>");
            break;
          }
        }else{
          Log.Info("this message is invalid: <<<"+ new JsonWriter().Write(msg)+">>>");
        }
      };

      _containingThread.Start();
    }

    private void onCtrlMessage(Dictionary<string,object> msg){
      Log.Info("the following message has been received: <<<"+ new JsonWriter().Write(msg)+">>>" );
      try{
        string command = JsonProtocolHelper.AssertTypeInDic<string>(msg, "command");
        switch(command){
        case "reportAllEvents":
          Log.Debug("dealing with the command reportAllEvents");
          String[] devices = JsonProtocolHelper.AssertTypeInDic<String[]>(msg, "params");
          StartAllEventsReporting(devices);
          break;
        default:
          break;
        }
      }catch (Exception ex){
        Log.Error("something was wrong with this control message: <<<"+new JsonWriter().Write(msg)+" >>> \n\tThe exception was: "+ex.Message );
      }
    }

    private void onEventMessage(Dictionary<string,object> msg){
      Log.Info("the following message has been received: <<<"+ new JsonWriter().Write(msg)+">>>" );
      //this is just a simple ugly draft: it needs to be done in a much better way later! this treatment need to be moved to the folder Command and to be sent to a CommandFactory and then apply from here
      String command  = JsonProtocolHelper.AssertTypeInDic<String>(msg, "command");
      Log.Debug (DateTime.Now.ToLongTimeString()+" >> dealing with the command: "+command);
      Dictionary<string,object> param = JsonProtocolHelper.AssertTypeInDic<Dictionary<String,Object>> (msg, "params");

      switch(command){
      case "show_color":
        this.ShowColor(param);
        break;
      case "show_json_picture":
        this.ShowJsonPicture(param);
        break;
      case "show_message":
        this.ShowMessage(param);
        break;
      default:
        break;
      }
    }

    private void StartAllEventsReporting(string[] devices){
      //TODO_LATER: move some of this code somewhere else to make it more readable
      AppManager mgr = AppManagerAccess.Instance;
      foreach(string cubeId in devices){
        try{
          Cube c  = mgr[cubeId];
          if(!CubeEventReporter.ExistsReporter(cubeId)){
            //CubeEventReporter cReporter = 
            new CubeEventReporter(this,c);
          }else{
            Log.Debug("this cube is already being reported");
          }
        } catch (KeyNotFoundException ex){
          Log.Error("the following id doesn't match any cube! --> "+cubeId+"\n\t exception message: "+ex.Message);
        }
      }
    }

    private delegate void OnCube(Cube c);
    private void BrowseCubes(OnCube method, String[] affectedCubes){
      CubeSet cubes = _appMgr.AvailableCubes;
      foreach(Cube c in cubes){
        if (Array.Exists (affectedCubes, delegate(String obj) {
          return obj.Equals (c.UniqueId);
        })){
          //TODO_LATER : remove the found Id of the affectedCubes array to speed up the process
          method(c);
        }
      }
    }

    private void ShowColor(Dictionary<string, object> param){
      //then read which color is asked
//      Dictionary<string,object> param = JsonProtocolHelper.AssertTypeInDic<Dictionary<String,Object>>(msg, "params");
      //read the consered cubes
      String[] affectedCubes = JsonProtocolHelper.AssertTypeInDic<String[]>(param, "cubes");
      //read the rgb value!
      Dictionary<string, object> colors = JsonProtocolHelper.AssertTypeInDic<Dictionary<String, Object>>(param, "color");
      Color fillingColor =
        new Color(
          JsonProtocolHelper.AssertTypeInDic<int>(colors,"r"),
          JsonProtocolHelper.AssertTypeInDic<int>(colors,"g"),
          JsonProtocolHelper.AssertTypeInDic<int>(colors,"b")
          );
      BrowseCubes( delegate(Cube c) {
          c.FillScreen(fillingColor);
          //TextDisplayer.DisplayMessage(c,"this is a color", new SiftColor(Color.White));
          c.Paint();
        }, affectedCubes);
    }
    private void ShowJsonPicture(Dictionary<string, object> param) {
//      Dictionary<string,object> param = JsonProtocolHelper.AssertTypeInDic<Dictionary<String,Object>> (msg, "params");
      String[] affectedCubes = JsonProtocolHelper.AssertTypeInDic<String[]>(param, "cubes");
      //JsonPicture picture = JsonPicture.createFromDictionary(JsonProtocolHelper.AssertTypeInDic<Dictionary<String, Object>>(param, "picture"));
      object objPicture = JsonProtocolHelper.AssertField(param, "picture");
      JsonPicture picture = new JsonReader().Read<JsonPicture>(new JsonWriter().Write(objPicture));
      BrowseCubes( delegate(Cube c) {
          ImageDisplayer.DisplayPicture(c, picture);
          //Log.Info("the picture is ready to be displayed on the cube!");
          Log.Debug (DateTime.Now.ToLongTimeString()+" before c.paint()");
          c.Paint ();
          Log.Debug (DateTime.Now.ToLongTimeString()+" after c.paint()");
        }, affectedCubes);

    }
    private void ShowMessage(Dictionary<string, object> param){
      String[] affectedCubes = JsonProtocolHelper.AssertTypeInDic<String[]>(param, "cubes");
      String text_msg = JsonProtocolHelper.AssertTypeInDic<String>(param, "text_msg");
      //Dictionary<string, object> colors = JsonProtocolHelper.AssertTypeInDic<Dictionary<String, Object>>(param, "color");
//      SiftColor textColor = new SiftColor(colors);
      SiftColor textColor = new SiftColor(255,255,255);
//      Color textColor =
//        new Color(
//          JsonProtocolHelper.AssertTypeInDic<int>(colors,"r"),
//          JsonProtocolHelper.AssertTypeInDic<int>(colors,"g"),
//          JsonProtocolHelper.AssertTypeInDic<int>(colors,"b")
//          );
      BrowseCubes(delegate(Cube c) {
        TextDisplayer.DisplayMessage(c, text_msg, textColor);
        c.Paint();
        }, affectedCubes);
    }



    private class JsonReaderThread {
      private volatile bool _running = true;
      private JsonTcpCommunication _communication;
      public delegate void IncomingMessageHandler(Dictionary<string,object> msg);

      public event IncomingMessageHandler IncomingMessage;

      public JsonReaderThread(JsonTcpCommunication comm){
        _communication = comm;

      }

      public void ReadingLoop(){
        Log.Debug("Reading loop starting!");

        if(_communication == null){
          return;
        }//else

        while(_running){
          //Log.Debug("reading a dictionary ... ");
          Dictionary<string, object> msg = _communication.Read();
          IncomingMessage(msg);//notify it!
        }
      }
    }
  }
}

