using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LerpPosition), typeof(Collider))]
public class CommanderUI : MonoBehaviour
{
    public event System.Action OnDraggingCommander = delegate { };
    public event System.Action OnStartDrag = delegate { };
    public event System.Action OnCommanderGrounded = delegate { };

    public delegate void BoardAction(TileData tile);
    public event BoardAction OnDropCommander = delegate { };
    public event BoardAction OnCommanderMoved = delegate { };

    public delegate void V3Action(Vector3 vec);
    public event V3Action OnCommanderDrop = delegate { };

    public Player _Player;
    public float _LiftedHeight;
    public float _LiftTime;
    public float _MoveTime;

    LerpPosition _lerpPosition;
    Vector3 _toGoTo;
    HashSet<TileData> _reachableTiles;
    TileHolder _destinationTile;
    Vector3 _destination;
    bool _liftingPiece;
    bool _hasBeenLifted;
    bool _allowMovement;
    bool _dragging;
    float _targetY;
    int _defaultLayer;

    bool _paused;
    public bool _Paused
    {
        get
        {
            return _paused;
        }

        set
        {
            if (value)
            {
                PausePlayerMovement();
                _allowMovement = false;
            }
            else
            {
                ContinuePlayerMovement();
                _allowMovement = true;
            }

            _paused = value;
        }
    }

	public Vector3 getPosition() {
		return _toGoTo;
	}
    
    // Use this for initialization
	public void Initialise () 
    {
        _lerpPosition = this.GetComponent<LerpPosition>();
        _defaultLayer = this.gameObject.layer;

        //event listener
        _lerpPosition.OnLerpFinished += _lerpPosition_OnLerpFinished;
	}

    void _lerpPosition_OnLerpFinished()
    {
        //if lifting piece then not doin it anymore
        if (_liftingPiece)
        {
            _liftingPiece = false;
            _hasBeenLifted = true;
        }
        else if (this.transform.position.y != _LiftedHeight)
        {
            OnCommanderGrounded();
            _hasBeenLifted = false;
        }
    }
	
	// Update is called once per frame
	void Update () 
    {
        //update the lerp goto point
        if (_hasBeenLifted && _lerpPosition.GetEndPosition() != _toGoTo)
        {
            _lerpPosition._LerpTime = _MoveTime;
            _lerpPosition.LerpTo(_toGoTo);
        }
	}

    void OnMouseDown()
    {
        //OnStartDrag();
    }

    void OnMouseDrag()
    {

		//only if movement is allowed
		if (_allowMovement) {

			if (!_dragging) {
				OnStartDrag();
				_dragging = true;
			}

			//if hovered a tile that is reachable then move have the player move there
			Collider tileParent;
            TileHolder tileHolder = null;
            if (BoardUI.GetTileHovered_Position(out tileParent))
                tileHolder = tileParent.GetComponentInChildren<TileHolder>();

            if (tileHolder != null)
            {

                //try get the commander position marker
                GameObject posMarker = getCommanderMarker(tileHolder);
                if (posMarker != null)
                {
                    _toGoTo = posMarker.transform.position;
                    _destination = _toGoTo;
                }
                else
                {
                    _toGoTo = tileHolder.transform.position;
                    _destination = _toGoTo;
                }

                _toGoTo.y = _LiftedHeight;

				_targetY = tileHolder._Tile.Height;

                _destinationTile = tileHolder;

                //dragging
                OnDraggingCommander();
            }
            else
            {
                _destinationTile = null;
            }

            if (!_hasBeenLifted && !_liftingPiece)// && _destinationTile == null )//|| _prevHovered != hoveredCollider))
				LiftPiece ();

            //block raycast 
            this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		}
    }

    void OnMouseUp()
    {
        _dragging = false;

		//if the tile hovered is not in the reachable set then back to origional tile
        if (_destinationTile == null || !_reachableTiles.Contains(_destinationTile._Tile))
        {
			//commander not moved
            GameObject posMarker = getCommanderMarker(_Player.CommanderPosition.TileObject.GetComponentInChildren<TileHolder>());
            _toGoTo = (posMarker!=null) ? posMarker.transform.position: _Player.CommanderPosition.TileObject.transform.position;
			_targetY = _Player.CommanderPosition.Height;
        }
        else
        {
            //commander moved
            _toGoTo = _destination;
            OnCommanderMoved(_destinationTile._Tile);
            OnCommanderDrop(new Vector3(_destination.x, _targetY, _destination.z));
            _destinationTile = null;
        }

        //drop the commander
        _toGoTo.y = _targetY;
        OnDropCommander(_Player.CommanderPosition);


        //raycast 
        this.gameObject.layer = _defaultLayer;
    }

    public void LiftPiece()
    {
        _liftingPiece = true;
        _lerpPosition._LerpTime = _LiftTime;
        _lerpPosition.LerpTo(new Vector3(this.transform.position.x, _LiftedHeight, this.transform.position.z));
        _toGoTo = _lerpPosition.GetEndPosition();
    }

    public void AllowPlayerMovement(HashSet<TileData> reachableTiles)
	{
		_allowMovement = true;
		_reachableTiles = reachableTiles;
	}

	public void DisablePlayerMovement()
	{
		_allowMovement = false;
	}

	public void PausePlayerMovement()
	{
		_lerpPosition.PauseLerp ();
	}

	public void ContinuePlayerMovement()
	{
		_lerpPosition.StartLerp ();
	}

    public void UpdateToPlayerPosition()
    {
        GameObject posMarker = getCommanderMarker(_Player.CommanderPosition.TileObject.GetComponentInChildren<TileHolder>());
        Vector3 newPosition = (posMarker != null) ? posMarker.transform.position : _Player.CommanderPosition.TileObject.transform.position;
		newPosition.y = _Player.CommanderPosition.Height;
        this.transform.position = newPosition;
        
        _lerpPosition.StopLerp();
    }

    public HashSet<TileData> GetReachableTiles()
    {
        return _reachableTiles;
    }

    GameObject getCommanderMarker(TileHolder tHolder)
    {
        if (tHolder == null)
            return null;

        switch (_Player.Type)
        {
            case PlayerType.None:
                Debug.LogError("Cannot get marker for player type None");
                break;
            case PlayerType.Battlebeard:
                return tHolder._MarkerCommanderBB;
            case PlayerType.Stormshaper:
                return tHolder._MarkerCommanderSS;
            default:
                Debug.LogError("Unhandled player type getting position marker");
                break;
        }

        return null;
    }
}
