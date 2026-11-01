using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Obi.Samples
{

    public class ToyController : MonoBehaviour
    {
        public enum Tool
        {
            Sand = 0, // sand permanently becomes mud when in contact with water. 
            Snow = 1, // snow reduces temperature, becomes water when in contact with fire.
            Water = 2, // water reduces temperature.
            Oil = 3, // oil keeps heat for a long time, unless mixed with water.
            Mud = 4, // mud permanently turns to sand when heated.
            Slime = 5, // slime loses viscosity when heated.
            Smoke = 6, // smoke disappears in contact with fire.
            Fire = 7,
            Lava = 8,
            Wood = 9, //wood wall: once past a certain temperature, increases temperature on its own. when too hot, falls down. when too hot for some time, disappears.
            Stone = 10, // stone wall: inert.
            Moss = 11,
            Rope = 12,
            Softball = 13,
            JellyCube = 14,
            Erase = 15,
            Attract = 16,
            Repel = 17,
            Push = 18,
        }

        public Text tooltip;
        public ToyTool[] tools;
        public Tool selectedTool = Tool.Sand;

        Vector3 prevPos;

        public void Start()
        {
            foreach (var t in tools)
                t.OnDeselect();
        }

        public void SelectEmitter(int index)
        {
            tools[(int)selectedTool].OnDeselect();
            selectedTool = (Tool)index;
            tooltip.text = tools[(int)selectedTool].tooltipText;
            tools[(int)selectedTool].OnSelect();
        }

        public void Update()
        {
            bool overUI = EventSystem.current.IsPointerOverGameObject();
            Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));

            if (Input.GetMouseButtonDown(0) && !overUI)
            {
                prevPos = pos;
                tools[(int)selectedTool].OnMouseDown(pos);
            }

            if (Input.GetMouseButton(0) && !overUI)
                tools[(int)selectedTool].OnMousePressed(pos, prevPos);
            
            if (Input.GetMouseButtonUp(0))
                tools[(int)selectedTool].OnMouseUp(pos);

            if (Vector3.Distance(pos, prevPos) > 0.1f)
                prevPos = pos;
        }

        public void Reset()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}