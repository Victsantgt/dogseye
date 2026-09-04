using System.Collections;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public GameObject[] SegmentMap;
    

    [SerializeField] int Zpos = 168; //cambiar segun el largo de el suelo

    [SerializeField] bool CreatingSegment = false; //si ya estamos creando un segmento, no queremos crear otro
    [SerializeField] int SegmentNum;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        if (CreatingSegment == false)
        {
            CreatingSegment = true;
            StartCoroutine(SegmentGen());
        }
        
    }

   IEnumerator SegmentGen()
    {
        SegmentNum = Random.Range(0, 3);
        Instantiate(SegmentMap[SegmentNum], new Vector3(0, 0, Zpos),Quaternion.identity);
        Zpos += 168;
        yield return new WaitForSeconds(5);
        CreatingSegment = false;

    }
   
}
