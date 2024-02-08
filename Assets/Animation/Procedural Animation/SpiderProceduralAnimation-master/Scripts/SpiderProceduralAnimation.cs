using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderProceduralAnimation : MonoBehaviour
{
    public Transform bodyTransform;
    public Transform[] legTargets;
    public float stepSize = 1f;
    public int smoothness = 1;
    public float stepHeight = 0.1f;
    public bool bodyOrientation = true;

    private float raycastRange = 1f;
    private Vector3[] defaultLegPositions;
    private Vector3[] lastLegPositions;
    private Vector3 lastBodyUp;
    private bool[] legMoving;
    private int nbLegs;
    
    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;

    private float velocityMultiplier = 15f;

    static Vector3[] MatchToSurfaceFromAbove(Vector3 point, float halfRange, Vector3 up) // Makes sure the leg target position is on the ground
    {
        Vector3[] res = new Vector3[2];
        RaycastHit hit;
        Ray ray = new Ray(point + halfRange * up, - up); // Creates a ray starting at the target leg position + an upwards vector pointing in a downwards direction
        
        if (Physics.Raycast(ray, out hit, 2f * halfRange)) // If the raycast hits the hitpoint position is returned
        {
            res[0] = hit.point;
            res[1] = hit.normal;
        }
        else // If the raycast doesn't hit then the original target position is returned
        {
            res[0] = point;
        }
        return res;
    }
    
    void Start() // Initialises all the positional/directional variables and populates the arrays
    {
        lastBodyUp = transform.up;

        //Arrays initialised at the length of how many legs have been assigned in the inspector
        nbLegs = legTargets.Length;
        defaultLegPositions = new Vector3[nbLegs];
        lastLegPositions = new Vector3[nbLegs];
        legMoving = new bool[nbLegs];

        for (int i = 0; i < nbLegs; ++i) // Populates the arrays with the current positions of the legs
        {
            defaultLegPositions[i] = legTargets[i].localPosition;
            lastLegPositions[i] = legTargets[i].position;
            legMoving[i] = false;
        }

        lastBodyPos = transform.position;
    }

    IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        Vector3 startPos = lastLegPositions[index]; // Set the starting postition to the legs current postition
        for(int i = 1; i <= smoothness; ++i) // Loop for the amount of times that smoothness is set to
        {
            // lerp from the last position to the target position by the (current index of the loop divided by the smoothness) which represents the decimal percentage of how far through the loop it is
            legTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (float)(smoothness + 1f)); // This means the higher the smoothness the more steps will be taken to move the leg from A to B

            /* Equation to get y point of an imaginary half circle over time, which creates the smooth up and down motion where: 
             * y = transform.up (y origin of circle) * sin() (sin() returns the vertical height/sine of the angle of the circle (index/smoothness * pi)) * stepHeight (controls the magnitude of the step) */ 
            legTargets[index].position += transform.up * Mathf.Sin(i / (float)(smoothness + 1f) * Mathf.PI) * stepHeight; 
            
            yield return new WaitForFixedUpdate(); // Waits until next physics step to continue
        }
        legTargets[index].position = targetPoint; // At the end of this loop the position is set to the target position due to inaccuracy in lerp where it will not end evenly  
        lastLegPositions[index] = legTargets[index].position; // A new last position is assigned for this leg based of its new position
        legMoving[0] = false;
    }


    void FixedUpdate() // Runs every fixed physics step
    {
        velocity = transform.position - lastBodyPos; // Calulates the current velocity of the spider
        velocity = (velocity + smoothness * lastVelocity) / (smoothness + 1f);

        if (velocity.magnitude < 0.000025f)
            velocity = lastVelocity;
        else
            lastVelocity = velocity;
        
        
        Vector3[] desiredPositions = new Vector3[nbLegs]; // Initialises an array at the length of theat the length of how many legs have been assigned in the inspector 
        int indexToMove = -1;
        float maxDistance = stepSize;

        for (int i = 0; i < nbLegs; ++i) // This loops calulates the leg furthest away from the its target and that is also more than the stepSize
        {
            desiredPositions[i] = transform.TransformPoint(defaultLegPositions[i]); // Populates this array with current world space positions of all the legs

            /* Returns the magnitude of the projected vector onto a plane defined by a normal at a right angle to the plane that passes through the world origin
             * (https://docs.unity3d.com/ScriptReference/Vector3.ProjectOnPlane.html) */
            float distance = Vector3.ProjectOnPlane(desiredPositions[i] + velocity * velocityMultiplier - lastLegPositions[i], transform.up).magnitude;

            if (distance > maxDistance) // Assigns the leg with the most distance from its target to be moved
            {
                maxDistance = distance;
                indexToMove = i;
            }
        }

        for (int i = 0; i < nbLegs; ++i) // For each leg that isn't the leg to be moved, its position is set to equal its last position; freezing it in place
            if (i != indexToMove)
                legTargets[i].position = lastLegPositions[i];

        if (indexToMove != -1 && !legMoving[0]) // If the current leg isn't index -1 and also isn't currently moving
        {
            // The target position is equal to the current world space position or WSP + (the velocity clamped * (WPS - LSP)) + velocity
            Vector3 targetPoint = desiredPositions[indexToMove] + Mathf.Clamp(velocity.magnitude * velocityMultiplier, 0.0f, 1.5f) * (desiredPositions[indexToMove] - legTargets[indexToMove].position) + velocity * velocityMultiplier;
            
            Vector3[] positionAndNormal = MatchToSurfaceFromAbove(targetPoint, raycastRange, transform.up); // Makes sure the leg target position is on the ground
            
            legMoving[0] = true; // Sets the leg to be moving

            StartCoroutine(PerformStep(indexToMove, positionAndNormal[0])); // Starts the coroutine to move the leg to the target position
        }

        // Orients the body upwards based on the normal of the average of all the leg positions
        lastBodyPos = transform.position;
        if (nbLegs > 3 && bodyOrientation)
        {
            Vector3 v1 = legTargets[0].position - legTargets[1].position;
            Vector3 v2 = legTargets[2].position - legTargets[3].position;
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(smoothness + 1));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(up), 1 * Time.deltaTime);
            lastBodyUp = up;
        }
    }

    private void OnDrawGizmosSelected() // Draws the gizmos in the scene view
    {
        for (int i = 0; i < nbLegs; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.05f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), stepSize);
        }
    }
}
