using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1
{
  internal class EntryPoint
  {
    public static void Main(string[] args)
    {
      var program = new Program(args);
    }
  }

  public class Program
  {
    public Program(string[] args)
    {
      var randomService = new RandomService();
      
      var stageFactory = new StageFactory(randomService);

      StageInfo stageInfo = new StageInfo()
      {
        RoomsCount = 100
      };
      
      StageData stage = stageFactory.Create(stageInfo);

      var stageView = new ConsoleStageView();
      
      stageView.ShowStage(stage);
    }
  }

  public class StageFactory
  {
    private RandomService _random;

    public StageFactory(RandomService random)
    {
      _random = random;
    }
    
    public StageData Create(StageInfo stageInfo)
    {
      StageShapeData shape = GenerateStageShape(stageInfo);

      StageData stage = FillStageByShape(shape, stageInfo);

      return stage;
    }
    
    private StageShapeData GenerateStageShape(StageInfo stageInfo)
    {
      int roomsCountLasts = stageInfo.RoomsCount;
      
      int stageGridSize = (int) Math.Sqrt(stageInfo.RoomsCount) * 2;
      StageShapeData shapeData = new StageShapeData
      {
        Shape = new bool[stageGridSize, stageGridSize]
      };

      HashSet<Vector2D> busyCells = new HashSet<Vector2D>();

      Vector2D currentCell = new Vector2D(
        x: (stageGridSize - stageGridSize % 2) / 2, 
        y: (stageGridSize - stageGridSize % 2) / 2);

      shapeData.StartRoomPosition = currentCell;

      shapeData.Shape[currentCell.X, currentCell.Y] = true;
      busyCells.Add(currentCell);

      --roomsCountLasts;

      while (roomsCountLasts > 0)
      {
        currentCell = _random.Choice(busyCells);

        HashSet<Vector2D> neighbors = GetNeighborCells(currentCell, stageGridSize);

        currentCell = _random.Choice(neighbors);
        
        if (shapeData.Shape[currentCell.X, currentCell.Y])
          continue;
        
        neighbors = GetNeighborCells(currentCell, stageGridSize);

        int freeNeighbors = neighbors.Count(neighbor => shapeData.Shape[neighbor.X, neighbor.Y] == false);
        
        if (neighbors.Count - freeNeighbors != 1)
          continue;
        
        shapeData.Shape[currentCell.X, currentCell.Y] = true;
        busyCells.Add(currentCell);
        
        --roomsCountLasts;
      }

      return shapeData;
    }

    private StageData FillStageByShape(StageShapeData stageShape, StageInfo stageInfo)
    {
      int stageGridSize = stageShape.Shape.GetLength(0);

      StageData stage = new StageData()
      {
        Rooms = new RoomData[stageGridSize, stageGridSize]
      };

      HashSet<Vector2D> endRooms = new HashSet<Vector2D>();

      for (int x = 0; x < stageGridSize; x++)
      {
        for (int y = 0; y < stageGridSize; y++)
        {
          stage.Rooms[x, y] = new RoomData();
          
          if (stageShape.Shape[x, y])
          {
            stage.Rooms[x, y].Doors = new RoomDoors
            {
              Up = x - 1 >= 0 && stageShape.Shape[x - 1, y],
              Down = x + 1 < stageGridSize && stageShape.Shape[x + 1, y],
              Right = y + 1 < stageGridSize && stageShape.Shape[x, y + 1],
              Left = y - 1 >= 0 && stageShape.Shape[x, y - 1]
            };

            if (new[]
                {
                  stage.Rooms[x, y].Doors.Up,
                  stage.Rooms[x, y].Doors.Right,
                  stage.Rooms[x, y].Doors.Down,
                  stage.Rooms[x, y].Doors.Left
                }.Count(b => b) == 1)
            {
              endRooms.Add(new Vector2D(x, y));
            }
            
            stage.Rooms[x, y].Type = RoomType.Enemies;
          }
          
          stage.Rooms[x, y].Position = new Vector2D(x, y);
        }
      }

      stage.Rooms[stageShape.StartRoomPosition.X, stageShape.StartRoomPosition.Y].Type = RoomType.Start;

      Vector2D furthestRoom = FindFurthestRoom(
        currentRoom: new Vector2D(stageShape.StartRoomPosition.X, stageShape.StartRoomPosition.Y),
        otherRooms: endRooms);

      stage.Rooms[furthestRoom.X, furthestRoom.Y].Type = RoomType.Downstairs;

      return stage;
    }

    private HashSet<Vector2D> GetNeighborCells(Vector2D currentCell, int stageGridSize)
    {
      HashSet<Vector2D> neighbors = new HashSet<Vector2D>();

      if (currentCell.Y + 1 < stageGridSize)
        neighbors.Add(new Vector2D(currentCell.X, currentCell.Y + 1));
      if (currentCell.Y - 1 >= 0)
        neighbors.Add(new Vector2D(currentCell.X, currentCell.Y - 1));
      if (currentCell.X + 1 < stageGridSize)
        neighbors.Add(new Vector2D(currentCell.X + 1, currentCell.Y));
      if (currentCell.X - 1 >= 0)
        neighbors.Add(new Vector2D(currentCell.X - 1, currentCell.Y));

      return neighbors;
    }

    private Vector2D FindFurthestRoom(Vector2D currentRoom, HashSet<Vector2D> otherRooms)
    {
      Vector2D furthestRoom = null;
      double furthestDistance = 0;
      
      foreach (Vector2D otherRoom in otherRooms)
      {
        int xDistance = currentRoom.X - otherRoom.X;
        int yDistance = currentRoom.Y - otherRoom.Y;

        double distance = Math.Sqrt(xDistance * xDistance + yDistance * yDistance);

        if (distance > furthestDistance)
        {
          furthestRoom = otherRoom;
          furthestDistance = distance;
        }
      }

      return furthestRoom;
    }
  }

  public abstract class StageView
  {
    public abstract void ShowStage(StageData stage);
  }

  public class MinimalisticConsoleStageView : StageView
  {
    public override void ShowStage(StageData stage)
    {
      Console.OutputEncoding = Encoding.UTF8;
      Console.InputEncoding = Encoding.UTF8;

      int stageGridSize = stage.Rooms.GetLength(0);
      
      for (int x = 0; x < stageGridSize; x++)
      {
        for (int y = 0; y < stageGridSize; y++)
        {
          Console.Write(GetCharByRoomType(stage.Rooms[x, y].Type));
        }
        
        Console.Write("\n");
      }

      Console.ReadKey();
    }

    private string GetCharByRoomType(RoomType type)
    {
      switch (type)
      {
        case RoomType.None:
          return "╔";
        case RoomType.Enemies:
          return " ";
        case RoomType.Start:
          return "S";
        case RoomType.Downstairs:
          return "D";
        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }
  
  public class ConsoleStageView : StageView
  {
    public override void ShowStage(StageData stage)
    {
      Console.OutputEncoding = Encoding.UTF8;
      Console.InputEncoding = Encoding.UTF8;

      int x, y, xx, yy;

      int stageGridSize = stage.Rooms.GetLength(0);

      char[,] stageView = new char[stageGridSize * 3, stageGridSize * 3];

      for (x = 0; x < stageGridSize; x++)
      {
        for (y = 0; y < stageGridSize; y++)
        {
          char[,] currentRoomView = new char[,]
          {
            {'╔', '═', '╗'},
            {'║', ' ', '║'},
            {'╚', '═', '╝'}
          };

          currentRoomView[1, 1] = GetCharByRoomType(stage.Rooms[x, y].Type);

          if (stage.Rooms[x, y].Doors.Up)
            currentRoomView[0, 1] = ' ';
          if (stage.Rooms[x, y].Doors.Right)
            currentRoomView[1, 2] = ' ';
          if (stage.Rooms[x, y].Doors.Down)
            currentRoomView[2, 1] = ' ';
          if (stage.Rooms[x, y].Doors.Left)
            currentRoomView[1, 0] = ' ';

          if (new[]
              {
                stage.Rooms[x, y].Doors.Up,
                stage.Rooms[x, y].Doors.Right,
                stage.Rooms[x, y].Doors.Down,
                stage.Rooms[x, y].Doors.Left
              }.Count(b => b) == 0)
          {
            currentRoomView = new char[,]
            {
              {' ', ' ', ' '},
              {' ', ' ', ' '},
              {' ', ' ', ' '}
            };
          }

          for (xx = 0; xx < 3; xx++)
          {
            for (yy = 0; yy < 3; yy++)
            {
              stageView[x * 3 + xx, y * 3 + yy] = currentRoomView[xx, yy];
            }
          }
        }
      }

      for (x = 0; x < stageGridSize * 3; x++)
      {
        Console.Write("\n");
          
        for (y = 0; y < stageGridSize * 3; y++)
        {
          Console.Write(stageView[x, y]);
        }
      }
      
      Console.ReadKey();
    }

    private char GetCharByRoomType(RoomType type)
    {
      switch (type)
      {
        case RoomType.None:
          return ' ';
        case RoomType.Enemies:
          return 'E';
        case RoomType.Start:
          return 'S';
        case RoomType.Downstairs:
          return 'D';
        default:
          throw new ArgumentOutOfRangeException(nameof(type), type, null);
      }
    }
  }

  public class RandomService
  {
    private readonly Random _rnd;
    
    public RandomService()
    {
      _rnd = new Random();
    }

    public bool GetBool() => 
      _rnd.NextDouble() >= 0.5;

    public int GetInt(int start, int end) => 
      _rnd.Next(start, end);

    public T Choice<T>(ICollection<T> collection) => 
      collection.ElementAt(_rnd.Next(collection.Count));
  }

  public class StageShapeData
  {
    public bool[,] Shape;
    public Vector2D StartRoomPosition;
  }

  public class StageInfo
  {
    public int RoomsCount;
  }

  public class StageData
  {
    public StageInfo Info;
    public RoomData[,] Rooms;
  }

  public class RoomData
  {
    public RoomDoors Doors = new RoomDoors();
    public RoomType Type = RoomType.None;

    public Vector2D Position;

    public RoomData()
    {
    }

    public RoomData(Vector2D position)
    {
      Position = position;
    }
  }

  [Serializable]
  public class RoomDoors
  {
    public bool Up;
    public bool Right;
    public bool Down;
    public bool Left;
  }

  public enum RoomType
  {
    None,
    Start,
    Enemies,
    Downstairs
  }

  public class Vector2D
  {
    public int X;
    public int Y;

    public Vector2D(int x, int y)
    {
      X = x;
      Y = y;
    }
  }
}