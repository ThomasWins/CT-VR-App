// using itk.simple;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRDirectInteractorCloseOnly : XRDirectInteractor
{
    [Tooltip("Maximum distance in meters to allow grab.")]
    public float grabRange = 0.04f;

    public override void GetValidTargets(List<IXRInteractable> validTargets)
    {
        base.GetValidTargets(validTargets);

        if (validTargets.Count > 0)
        {
            IXRInteractable closest = null;
            float bestDistance = float.MaxValue;

            foreach (var t in validTargets)
            {
                // Get the GameObject of the interactable
                GameObject go = t.transform.gameObject;

                Collider col = go.GetComponent<Collider>();
                if (col == null) continue;

                Vector3 closestPoint = col.ClosestPoint(transform.position);
                float dist = Vector3.Distance(closestPoint, transform.position);

                if (dist < bestDistance && dist <= grabRange)
                {
                    bestDistance = dist;
                    closest = t;
                }
            }

            validTargets.Clear();
            if (closest != null)
                validTargets.Add(closest);
        }
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        if (interactable == null) return false;

        GameObject go = interactable.transform.gameObject;

        Collider col = go.GetComponent<Collider>();
        if (col == null)
            return false;

        float dist = Vector3.Distance(col.ClosestPoint(transform.position), transform.position);
        if (dist > grabRange)
            return false;

        return base.CanSelect(interactable);
    }
}
