using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveRaycast : MonoBehaviour
{
    [SerializeField] float _movePower = 0.01f;
    [SerializeField] GameObject _markerObj = null;
    bool _isMove = false;
    Vector3 _targetPos = Vector3.zero;

    void Start()
    {
        
    }
    
    void Update()
    {
        //移動
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool is_hit = Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground"));
            if (is_hit)
            {
                _isMove = true;
                _targetPos = hit.point;
                _markerObj.transform.position = hit.point;
                //this.transform.LookAt(_targetPos, Vector3.up);
            }
        }
        
        if(_isMove)
        {
            Vector3 moveVec = _targetPos - this.transform.position;
            moveVec.y = 0; //Y軸は移動しない
            if (moveVec.magnitude < _movePower * _movePower)
            {
                moveVec = Vector3.zero;
                _isMove = false;
            }
            this.transform.position += moveVec.normalized * _movePower;
        }
    }
}
