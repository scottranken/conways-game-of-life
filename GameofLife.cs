/**
 * Implementing Conway's Game of Life written in C# using Godot v3.6
 * 
 * Author: Scott Ranken
 * Created: 2025-07-12
 *
 * Description
 * Overview: Implements Conway's Game of Life by creating each cell on initialisation
 *           and changing their alpha values to represent alive and dead states. UI elements 
 *           and user input are included to manipulate the Game of Life settings.
 *
 * Cells:	 Each cell is created on initialisation and has a 33% chance to start as alive. 
 *           Instead of deleting and recreating cells each frame, they are created once and 
 *           their alpha values are adjusted to reflect their alive or dead state.
 *           See maintainGrid() for the Game of Life logic.
 *	
 * Grid:  	 Creates the background and lines that make up the Game of Life grid.
 *
 * GridUI:	 Creates the UI elements that allow users to change the Game of Life state. These
 *           include a slider for the time between generations, a pause/play button, a reset
 *           button, and a spinbox to enter the starting alive percentage for cell creation.
 *			 The number of alive cells and the current generation are also displayed.
 */

using Godot;
using System;

public class GameofLife : Node2D
{
	const int gridX 					= 18;
	const int gridY 					= 20;

	const float cellX 					= 20f;
	const float cellY 					= 20f;
	
	const float borderWidth 			= 2f;
	const float controlHeight 			= 95f;

	const float cellColourR 			= 0f;
	const float cellColourG 			= 150f;
	const float cellColourB 			= 30f;

	Random random 						= new Random();
	Cell[,] cells 						= new Cell[gridX, gridY];

	private GridUI gridUI;
	private static Timer timer;

	private bool isRunning 				= true;
	
	private static int generation 		= 0;
	private static int aliveCells 		= 0;
	private static float alivePercent 	= 33f;

	public class Cell : Reference
	{
		private GameofLife _game;
		private ColorRect _cell;
		private bool _alive = false;
		private bool _nextAliveState = false;

		public Cell(GameofLife game, float x, float y, float cellWidth, float cellHeight, float r, float g, float b, bool alive)
		{
			_game 				= game;
			_alive 				= alive;
			_cell 				= new ColorRect();
			_cell.RectPosition 	= new Vector2(x, y);
			_cell.RectMinSize 	= new Vector2(cellWidth, cellHeight);
			_cell.Color			= getRGBAValues(r, g, b, alive ? 255f : 0f);
			_cell.MouseFilter	= Control.MouseFilterEnum.Stop;
			
			// For user input
			_cell.SetMeta("cell", this);
			_cell.Connect("gui_input", _game, nameof(GameofLife.OnCellClicked), new Godot.Collections.Array { this });

		}
		
		public bool getCellAliveState()
		{
			return _alive;
		}
		
		public void setCellAliveState(bool alive)
		{
			_alive = alive;
			_cell.Color = getRGBAValues(GameofLife.cellColourR, GameofLife.cellColourG, GameofLife.cellColourB, alive ? 255f : 0f);
		}
		
		public bool getCellNextAliveState()
		{
			return _nextAliveState;
		}
		
		public void setCellNextAliveState(bool nextAliveState)
		{
			_nextAliveState = nextAliveState;
		}
		
		public ColorRect getCell()
		{	
			return _cell;
		}
	}
	
	public void OnCellClicked(InputEvent @event, Cell cell)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == (int)ButtonList.Left)
		{
			bool newState = !cell.getCellAliveState();
			cell.setCellNextAliveState(newState);
			cell.setCellAliveState(newState);
			
			if (newState)
			{
				aliveCells++;
			}
			else
			{
				aliveCells--;
			}
		}
	}
	
	public class Grid
	{
		private GameofLife _parent;
		private float _canvasX;
		private float _canvasY;
		private float _controlHeight;
		
		public Grid(GameofLife parent, float canvasX, float canvasY, float controlHeight)
		{
			_parent 		= parent;
			_canvasX 		= canvasX;
			_canvasY 		= canvasY;
			_controlHeight 	= controlHeight;
		}

		public void DrawGrid()
		{
			var background = new Cell(_parent, 0, 0, _canvasX, _canvasY+_controlHeight, 0f, 0f, 0f, true);
			background.getCell().MouseFilter = Control.MouseFilterEnum.Ignore;
			_parent.AddChild(background.getCell());

			_parent.AddChild(CreateLine(0f, _canvasY, 0f, _canvasY+_controlHeight, borderWidth, 255f, 255f, 255f));
			_parent.AddChild(CreateLine(_canvasX, _canvasY, _canvasX, _canvasY+_controlHeight, borderWidth, 255f, 255f, 255f));
			_parent.AddChild(CreateLine(0f, _canvasY+_controlHeight, _canvasX, _canvasY+_controlHeight, borderWidth, 255f, 255f, 255f));
			
			for (int i = 0; i <= gridX; i++)
			{
				float x = i*cellX;
				_parent.AddChild(CreateLine(x, 0, x, gridY*cellY, borderWidth, 255f, 255f, 255f));
			}
			
			for (int j = 0; j <= gridY; j++)
			{
				float y = j * cellY;
				_parent.AddChild(CreateLine(0, y, gridX*cellX, y, borderWidth, 255f, 255f, 255f));
			}
		}

		private Line2D CreateLine(float startingX, float startingY, float endingX, float endingY, float width, float r, float g, float b, float alpha = 255)
		{
			Line2D line = new Line2D();
			line.AddPoint(new Vector2(startingX, startingY));
			line.AddPoint(new Vector2(endingX, endingY));
			line.Width = width;
			line.DefaultColor = getRGBAValues(r, g, b, alpha);
			return line;
		}
	}

	public class GridUI
	{
		private Label _cellsLabel;
		private Label _generationLabel;
		private Label _timerLabel;
		private Label _alivePercentLabel;
		private HSlider _hSlider;
		private TextureButton _pausePlayButton;
		private TextureButton _resetButton;
		private Texture _playIcon;
		private Texture _pauseIcon;
		private Texture _resetIcon;
		private SpinBox _alivePercentSpinBox;
		
		public GridUI(Node parent)
		{
			_playIcon = (Texture)GD.Load("res://play.png");
			_pauseIcon = (Texture)GD.Load("res://pause.png");
			_resetIcon = (Texture)GD.Load("res://reset.png");
			_cellsLabel = (Label)parent.GetNode("UI/CellsLabel");
			_generationLabel = (Label)parent.GetNode("UI/GenerationLabel");
			_timerLabel = (Label)parent.GetNode("UI/TimerLabel");
			_alivePercentLabel = (Label)parent.GetNode("UI/AlivePercentLabel");
			_hSlider = (HSlider)parent.GetNode("UI/HSlider");
			_pausePlayButton = (TextureButton)parent.GetNode("UI/PausePlayButton");
			_resetButton = (TextureButton)parent.GetNode("UI/ResetButton");
			_alivePercentSpinBox = (SpinBox)parent.GetNode("UI/AlivePercentSpinBox");
			
			
			_timerLabel.Text = "                Speed\n0s                                    2s";
			_alivePercentLabel.Text = "Starting Cell Alive Percentage: ";
			//_alivePercentSpinBox.Value = 33f;
			
			setButtonProperties(_pausePlayButton, _pauseIcon, 0.115f, 0.115f, 10);
			setButtonProperties(_resetButton, _resetIcon, 0.105f, 0.105f, 10);
		}
		
		private void setButtonProperties(TextureButton button, Texture texture, float scaleX, float scaleY, int zIndex)
		{
			button.TextureNormal = texture;
			button.RectScale = new Vector2(scaleX, scaleY);
			button.Set("z_index", zIndex);
		}
		
		public void initialise()
		{
			_alivePercentSpinBox.Value = 33f;
		}
		
		public Label getCellsLabel()
		{
			return _cellsLabel;
		}
		
		public Label getGenerationLabel()
		{
			return _generationLabel;
		}
		
		public void SetPausedIcon(bool paused)
		{
			_pausePlayButton.TextureNormal = paused ? _playIcon : _pauseIcon;
		}
	}
	
	private static Color getRGBAValues(float r, float g, float b, float a)
	{
		return new Color(r/255f, g/255f, b/255f, a/255f);
	}
	
	private float getRandomFloat(float min, float max, Random rand)
	{
		return min+(float)(rand.NextDouble()*(max - min));
	}
	
	private void initialiseTimer()
	{
		timer = (Timer)GetNode("UI/Timer");
		timer.Connect("timeout", this, nameof(OnTimerTimeout));
		timer.WaitTime = 1;
		timer.Start();
	}
	
	private void resetTimer()
	{
		timer.Stop();
		if (isRunning)
		{
			timer.Start();
		}
	}
	
	private void _on_HSlider_value_changed(float value)
	{
		timer.WaitTime = value / 1000f;
		resetTimer();
	}
	
	private void _on_PausePlayButton_pressed()
	{
		isRunning = !isRunning;
		if (isRunning)
		{
			timer.Start();
			gridUI.SetPausedIcon(false);
		}
		else
		{
			timer.Stop();
			gridUI.SetPausedIcon(true);
		}
	}
	
	private void _on_ResetButton_pressed()
	{
		generation 	= 0;
		aliveCells 	= 0;
		
		resetGrid();
		resetTimer();
	}
	
	private void _on_AlivePercentSpinBox_value_changed(float value)
	{
		alivePercent = value;
	}
	
	private void OnTimerTimeout()
	{
		maintainGrid();
	}
	
	private int getAliveNeighbourCells(int x, int y)
	{
		// Was previosuly using a nested for loop to check the neighbouring cells.
		// Unrolled to check manually as using this function inside an existing nested loop.
		
		int aliveNeighbourCells = 0;
		
		// Top left
		if (x > 0 && y > 0 && cells[x-1,y-1].getCellAliveState())
		{
			aliveNeighbourCells++;
		}
		
		// Top
		if (x > 0 && cells[x-1,y].getCellAliveState())
		{
			aliveNeighbourCells++;
		}
		
		// Top right
		if (x > 0 && y < gridY-1 && cells[x-1,y+1].getCellAliveState())
		{
			aliveNeighbourCells++;
		}
		
		// Left
		if (y > 0 && cells[x,y-1].getCellAliveState())
		{
			aliveNeighbourCells++;
		}
		
		// Right
		if (y < gridY-1 && cells[x,y+1].getCellAliveState())
		{
			aliveNeighbourCells++;
		}
		
		// Bottom left
		if (x < gridX-1 && y > 0 && cells[x+1,y-1].getCellAliveState()) 
		{
			aliveNeighbourCells++;
		}
		
		// Bottom
		if (x < gridX-1 && cells[x+1,y].getCellAliveState()) 
		{
			aliveNeighbourCells++;
		}
		
		// Bottom right
		if (x < gridX-1 && y < gridY-1 && cells[x+1,y+1].getCellAliveState())
		{
			aliveNeighbourCells++;
		}
		
		return aliveNeighbourCells;
	}
	
	private void initialiseGrid()
	{
		// Create each cell on initialise. Each cells alives state is visualised via their alpha level.
		for (int x = 0; x < gridX; x++)
		{
			for (int y = 0; y < gridY; y++)
			{
				// Starting percent of each cell being alive on creation.
				if (getRandomFloat(1f, 100f, random) <= alivePercent)
				{
					cells[x,y] = new Cell(this, x*cellX+1f, y*cellY+1f, cellX-2f, cellY-2f, cellColourR, cellColourG, cellColourB, true);
				}
				else
				{
					cells[x,y] = new Cell(this, x*cellX+1f, y*cellY+1f, cellX-2f, cellY-2f, cellColourR, cellColourG, cellColourB, false);
				}
				AddChild(cells[x,y].getCell());
			}
		}
	}

	private void maintainGrid()
	{
		// Set each cells next state on the first grid iteration. Do not update cell
		// alive states while iterating as this will change neighbouring cells behaviour.
		for (int x = 0; x < gridX; x++)
		{
			for (int y = 0; y < gridY; y++)
			{
				int aliveNeighbourCells = getAliveNeighbourCells(x,y);
				if (cells[x,y].getCellAliveState())
				{
					// 2. Any live cell with two or three live neighbours lives on to the next generation.
					if (aliveNeighbourCells >= 2 && aliveNeighbourCells <= 3)
					{
						continue;
					}
					// 1. Any live cell with fewer than two live neighbours dies, as if by underpopulation.
					// 3. Any live cell with more than three live neighbours dies, as if by overpopulation.
					if (aliveNeighbourCells < 2 || aliveNeighbourCells > 3)
					{
						cells[x,y].setCellNextAliveState(false);
					}
				}
				else
				{
					// 4. Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
					if (aliveNeighbourCells == 3)
					{
						cells[x,y].setCellNextAliveState(true);
					}
				}
			}
		}
		
		// Update the entire grids alive state
		aliveCells = 0;
		for (int x = 0; x < gridX; x++)
		{
			for (int y = 0; y < gridY; y++)
			{
				bool alive = cells[x,y].getCellNextAliveState();
				cells[x,y].setCellAliveState(alive);
				if (alive)
				{
					aliveCells++;
				}
			}
		}
		generation++;
	}
	
	private void resetGrid()
	{
		for (int x = 0; x < gridX; x++)
		{
			for (int y = 0; y < gridY; y++)
			{
				if (getRandomFloat(1f, 100f, random) <= alivePercent)
				{
					cells[x,y].setCellAliveState(true);
					cells[x,y].setCellNextAliveState(true);
				}
				else
				{
					cells[x,y].setCellAliveState(false);
					cells[x,y].setCellNextAliveState(false);
				}
			}
		}
	}
	
	public override void _Ready()
	{
		Grid grid = new Grid(this, gridX*cellX, gridY*cellY, controlHeight);
		grid.DrawGrid();

		gridUI = new GridUI(this);
		
		initialiseTimer();
		initialiseGrid();
	}
	
	public override void _Process(float delta)
	{
		gridUI.getCellsLabel().Text = $"Cells: {aliveCells}";
		gridUI.getGenerationLabel().Text = $"Generation: {generation}";
	}
}
