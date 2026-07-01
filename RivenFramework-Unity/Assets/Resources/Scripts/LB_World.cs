//==========================================( Neverway 2025 )=========================================================//
// Author
//  Liz M.
// 
// Contributors: 
//  Connorses, Errynei, Soulex
//
//====================================================================================================================//

using System;
using UnityEngine;

/// <summary>
///  This is a Level Blueprint (LB) script, it is attached to the WorldSettings
///  object in a scene.
///  This LB makes the HUD widget appear on game maps.
/// </summary>
public class LB_World : MonoBehaviour
{
    #region========================================( Variables )======================================================//
    /*-----[ Inspector Variables ]------------------------------------------------------------------------------------*/
    public GameObject levelRoot;
    public float levelLength;
    public bool wrapLevel;
    
    
    /*-----[ External Variables ]-------------------------------------------------------------------------------------*/

    
    /*-----[ Internal Variables ]-------------------------------------------------------------------------------------*/

    
    /*-----[ Reference Variables ]------------------------------------------------------------------------------------*/
    private GI_WidgetManager widgetManager;
    private AerowingController aerowingController;
    [Tooltip("A reference to the HUD widget prefab to draw to the UI")]
    [SerializeField] private GameObject HUDWidgetPrefab;
    
    #endregion
    
    
    #region=======================================( Functions )=======================================================//
    
    /*-----[ Mono Functions ]-----------------------------------------------------------------------------------------*/
    private void Start()
    {
        //widgetManager = FindObjectOfType<GI_WidgetManager>();
        //widgetManager.AddWidget(HUDWidgetPrefab);
        aerowingController = FindObjectOfType<AerowingController>();
    }

    private void FixedUpdate()
    {
        var currentLevelProgress = -aerowingController.position.z;
        if (wrapLevel) currentLevelProgress = -aerowingController.position.z % (levelLength / aerowingController.WORLD_SCALE);
        levelRoot.transform.position = new Vector3(0, 0, currentLevelProgress) * aerowingController.WORLD_SCALE;
    }

    /*-----[ Internal Functions ]-------------------------------------------------------------------------------------*/
    
    
    /*-----[ External Functions ]-------------------------------------------------------------------------------------*/
    
    
    #endregion
    
}