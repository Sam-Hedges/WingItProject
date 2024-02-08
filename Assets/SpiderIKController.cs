using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderIKController : MonoBehaviour
{

    public List<FootIKTarget> footIKTargets = new List<FootIKTarget>(4); // The IK points for the position of the feet

    [System.Serializable] public struct FootPosTargets { public Transform parent; public FootPosTarget child; } // A struct for the foot targets to store the parents and children in one variable
    [SerializeField] public List<FootPosTargets> footPosTargets = new List<FootPosTargets>(4); // The targets for the position of the feet

    [SerializeField] public List<bool> activeFoot = new List<bool>(4);

    public LayerMask layerMask; // Raycast layermask to select the ground
    public Transform rootBone; // Root bone of the spider model

    public AnimationCurve stepCurveX; //
    public AnimationCurve stepCurveY; //animation curves to control foot motion while taking steps
    public AnimationCurve stepCurveZ; //

    public AnimationCurve stepOffsetCurveX; //
    public AnimationCurve stepOffsetCurveY; //just extra curves to make the animation look nicer. These curves must always start and end at zero
    public AnimationCurve stepOffsetCurveZ; //in order for them to have NO net effect on the animation

    public float stepDistance; // The distance it takes before the foot is told to move to the new spot
    public float stepTime = 0.5f;
    public Vector3 bodyOffset = Vector3.zero; // The spider bodies offset from the average position it is placed in

    public Color GizmoColor = Color.red;


    
    private void OnEnable()
    {
        for (int i = 0; i < footIKTargets.Count; i++)
        {
            RaycastHit hit; // Creates blank raycast hit point

            // Casts a ray down from the current foot IK pos to get the ground position underneath, and then assigns the foot IK pos to equal the ground pos 
            if (Physics.Raycast(footIKTargets[i].tip.position, Vector3.down, out hit, 100f)) { footIKTargets[i].position = hit.point; }

            footIKTargets[i].stablePosition = footIKTargets[i].position;
        }
    }

    private void Start()
    {
        for (int i = 0; i < footIKTargets.Count; i++) { footIKTargets[i].position = footIKTargets[i].stablePosition; }
    }

    // Update is called once per frame
    void Update()
    {
        FootTargets();

        for (int i = 0; i < footIKTargets.Count; i++)
        {
            float dist = Vector3.Distance(footIKTargets[i].stablePosition, footPosTargets[i].child.position);


            if (dist < stepDistance)
            {
                footIKTargets[i].position = footIKTargets[i].stablePosition;
            } 
            else if ((dist >= stepDistance) && !activeFoot[i])
            {               
                footPosTargets[i].child.stablePosition = footPosTargets[i].child.position;

                activeFoot[i] = true;
                activeFoot[CounterFoot(i)] = true;
                activeFoot[CounterFootHind(i)] = true;

                StartCoroutine(MoveFoot(i)); // Starts the coroutine that moves the foot
            }
        }
    }


    IEnumerator MoveFoot(int limb)
    {
        float i = 0;
        float rate = 1 / stepTime;

        // While the foot position doesnt equal the target position
        while (footIKTargets[limb].position != footPosTargets[limb].child.stablePosition)
        {
            i += rate * Time.deltaTime;

            //Obtain the non-animated world step (length between new foot pos and current foot pos)
            Vector3 worldStepVector = footPosTargets[limb].child.stablePosition - footIKTargets[limb].position;
            float horizontalStepMagnitude = new Vector2(worldStepVector.x, worldStepVector.z).magnitude;

            //Convert world step vector to a local step vector with local forward direction as reference forward direction
            float stepAngleOffset = Vector2.SignedAngle(new Vector2(worldStepVector.x, worldStepVector.z), new Vector2(transform.forward.x, transform.forward.z));
            Vector3 localStepVector = new Vector3(Mathf.Sin(stepAngleOffset * Mathf.Deg2Rad) * horizontalStepMagnitude, worldStepVector.y, Mathf.Cos(stepAngleOffset * Mathf.Deg2Rad) * horizontalStepMagnitude);


            //Sample animation curves taking note of the limb side. *The animation curves are designed with the world forward direction as reference forward direction
            int sideSign = footIKTargets[limb].limbSide == LimbSide.Left ? 1 : -1;
            Vector3 stepProgress = new Vector3(stepCurveX.Evaluate(i) * sideSign, stepCurveY.Evaluate(i), stepCurveZ.Evaluate(i));
            Vector3 stepOffset = new Vector3(stepOffsetCurveX.Evaluate(i) * sideSign, stepOffsetCurveY.Evaluate(i), stepOffsetCurveZ.Evaluate(i));

            //compute the animated step vector from the local step vector
            Vector3 animatedLocalStepVector = Vector3.Scale(localStepVector, stepProgress) + stepOffset;

            float animatedHorizontalStepMagnitude = new Vector2(animatedLocalStepVector.x, animatedLocalStepVector.z).magnitude;
            float animationAngleOffset = Vector2.SignedAngle(new Vector2(animatedLocalStepVector.x, animatedLocalStepVector.z), new Vector2(localStepVector.x, localStepVector.z));

            //Obtain the angle between the world forward direction and the local forward direction
            float forwardAngleOffset = Vector2.SignedAngle(new Vector2(transform.forward.x, transform.forward.z), Vector2.up);

            float totalAngleOffset = stepAngleOffset + animationAngleOffset + forwardAngleOffset;

            Vector3 animatedWorldStepVector = new Vector3(Mathf.Sin(totalAngleOffset * Mathf.Deg2Rad) * animatedHorizontalStepMagnitude, animatedLocalStepVector.y, Mathf.Cos(totalAngleOffset * Mathf.Deg2Rad) * animatedHorizontalStepMagnitude);

            footIKTargets[limb].position += animatedWorldStepVector;

            // Waits until next frame
            yield return null;
        }

        footIKTargets[limb].stablePosition = footPosTargets[limb].child.stablePosition;
        footIKTargets[limb].position = footIKTargets[limb].stablePosition;

        activeFoot[CounterFoot(limb)] = false;
        activeFoot[CounterFootHind(limb)] = false;

        //BodyPosition();

        // Stops the coroutine
        yield break;
    }

    // Gets the oppsite foot used of the currently selected foot (to make sure that the foot stays grounded when the other is moving)
    private int CounterFoot(int i)
    {
        // If remainder of the current foot index divided by 2 is 0 and thus even, then return -1, else it must be odd and so return 1 (added to current index to get opposite foot)
        int count = i + 1 + (((i % 2) == 0) ? -1 : 1);
        return count;
    }

    private int CounterFootHind(int i)
    {
        // If remainder of the current foot index divided by 2 is 0 and thus even, then return -1, else it must be odd and so return 1 (added to current index to get opposite foot)
        int count = i + 1 + (((i % 2) == 0) ? -1 : 1);

        count += count <= 2 ? 2 : -2;

        return count;
    }

    private void FootTargets()
    {
        // Places the foot targets
        for (int i = 0; i < footPosTargets.Count; i++)
        {
            RaycastHit hit; // Creates blank raycast hit point

            // Casts a ray down from the current foot IK target pos to get the ground position underneath, and then assigns the foot IK target pos to equal the ground pos 
            if (Physics.Raycast(footPosTargets[i].parent.position, Vector3.down, out hit, 100f)) { footPosTargets[i].child.position = hit.point; }

                
        }
    }


    private void BodyPosition()
    {
        //Store all foot target positions before moving their parent, which is this transform
        Vector3[] tempTargetPositions = new Vector3[footIKTargets.Count];

        //Calculate the average feet position
        Vector3 cummulativeFeetPos = Vector3.zero;

        // Calculating the averages
        for (int i = 0; i < footIKTargets.Count; i++)
        {
            tempTargetPositions[i] = footIKTargets[i].stablePosition;

            cummulativeFeetPos += footIKTargets[i].stablePosition;
        }

        Vector3 averageStableFeetPos = cummulativeFeetPos / (footIKTargets.Count);

        //The position of the body is the average stable feet positions + the body offset
        rootBone.position = rootBone.InverseTransformPoint(averageStableFeetPos) + bodyOffset;


        //Restore foot target positions to cancel out parent's movement
        for (int i = 0; i < footIKTargets.Count; i++)
        {
            footIKTargets[i].position = tempTargetPositions[i];
        }
    }  

}
