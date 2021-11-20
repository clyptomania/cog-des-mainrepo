using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShaderBehaviour : MonoBehaviour
{
    public Material grid_mat;

    public enum shaderPhase
    {
        none,
        validation,
        fixation,
    }

    private static readonly int TargetCircle = Shader.PropertyToID("_TargetCircle");
    private static readonly int QuadRatio = Shader.PropertyToID("_QuadRatio");
    private static readonly int NRows = Shader.PropertyToID("_nRows");
    private static readonly int NCols = Shader.PropertyToID("_nCols");

    private shaderPhase _phase;
    public shaderPhase phase
    {
        get { return _phase; }
        set
        {
            _phase = value;

            print(string.Format("shader phase {0}", _phase));

            switch (_phase)
            {
                case shaderPhase.none:
                    _renderer.material = grid_mat;

                    _renderer.material.SetInt(NRows, 3);
                    _renderer.material.SetInt(NCols, 3);
                    _renderer.material.SetInt(TargetCircle, -1);

                    gameObject.SetActive(false);

                    break;
                case shaderPhase.validation:
                    gameObject.SetActive(true);

                    _renderer.material = grid_mat;

                    // _renderer.material.SetVector("_rangeX", gridCentering);
                    // _renderer.material.SetVector("_rangeY", gridCentering);

                    _renderer.material.SetInt(NRows, 3);
                    _renderer.material.SetInt(NCols, 3);
                    _renderer.material.SetInt(TargetCircle, -1);

                    StartCoroutine("ValidationProcedure");

                    break;

                    // case shaderPhase.fixation:
                    // 	_renderer.material = grid_mat;

                    // 	_renderer.material.SetInt("_nRows", 3);
                    // 	_renderer.material.SetInt("_nCols", 3);
                    // 	_renderer.material.SetInt("_TargetCircle", 4);

                    // 	StartCoroutine("FixationProcedure", Utils.SubjFoV.dim/2);

                    // 	break;
            }
            _renderer.material.SetFloat(QuadRatio, aspectRatio);
        }
    }

    //	[SerializeField] public static readonly Vector2 gridCentering;

    private MeshRenderer _renderer;

    [SerializeField]
    private ExpeControl controller;
    private float aspectRatio;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();

        Camera cam = GetComponentInParent<FillCamFoV>().mainCam;
        aspectRatio = cam.aspect;

    }

    public Stack<int> ringValidationStack;
    private static Stack<int> getRingsList(int nrings)
    {
        List<int> ringL = new List<int>(nrings);
        for (int i = 0; i < nrings; i++)
            ringL.Add(i);

        Utils.ShuffleList(ringL);
        return new Stack<int>(ringL);
    }

    public struct fixationResult
    {
        public bool success;
        public string eye;
        public long nSamples;
        public float mean;
        public float std;
    }

    public delegate void fixationCallbackDel(fixationResult res);
    public fixationCallbackDel fixationCallback = res => { };

    [SerializeField, Tooltip("max ring duration")] private float ringDuration; // msec
    [SerializeField, Tooltip("in degrees of FoV")] private float accThreshold; // degrees

    IEnumerator FixationProcedure(Vector2 targetPos)
    {
        List<Vector2> Lsamples = new List<Vector2>(500);
        List<Vector2> Rsamples = new List<Vector2>(500);

        print(string.Format("Fixation target : {0}", targetPos));

        // controller sampling callback
        controller.SamplingCallbacks.Add("fixation",
            gaze =>
            {
                if (gaze.valid(ExpeControl.lateralisation.left))
                {
                    if (!float.IsNaN(ExpeControl.instance.validationHit[0].x))
                    {
                        Lsamples.Add(ExpeControl.instance.validationHit[0]);
                    }
                }

                if (gaze.valid(ExpeControl.lateralisation.right))
                {
                    if (!float.IsNaN(ExpeControl.instance.validationHit[1].x))
                    {
                        Rsamples.Add(ExpeControl.instance.validationHit[1]);
                    }
                }
            }
        );

        long[] nSamples = { 0, 0 };
        float[] accL = { 0, 0, 0 }, accR = { 0, 0, 0 };

        long time = ExpeControl.getTimeStamp();
        yield return new WaitForSecondsRealtime(.5f);
        while ((ExpeControl.getTimeStamp() - time) < ringDuration)
        {
            //			print(string.Format("delta: {0}", controller.getTimeStamp()-time));
            // Only measure data sampled during the last second or so
            int nTotalSamples = 120;
            if (Lsamples.Count > nTotalSamples)
            {
                nSamples[0] += Lsamples.Count - nTotalSamples;
                Lsamples.RemoveRange(0, Lsamples.Count - nTotalSamples);
            }
            if (Rsamples.Count > nTotalSamples)
            {
                nSamples[1] += Rsamples.Count - nTotalSamples;
                Rsamples.RemoveRange(0, Rsamples.Count - nTotalSamples);
            }

            // mean, std
            accL = Utils.getFixationAccuracy(new List<Vector2>(Lsamples), targetPos);
            accR = Utils.getFixationAccuracy(new List<Vector2>(Rsamples), targetPos);

            if (accL[0] < 1.7f && accR[0] < 1.7f) break;

            yield return new WaitForSecondsRealtime(.2f);
        }

        print(string.Format("duration: {0}", ExpeControl.getTimeStamp() - time));

        nSamples[0] += Lsamples.Count;
        nSamples[1] += Rsamples.Count;

        // Report via callback
        fixationCallback(new fixationResult { eye = "left", success = accL[0] < accThreshold, mean = accL[0], std = accL[1], nSamples = nSamples[0] });
        fixationCallback(new fixationResult { eye = "right", success = accR[0] < accThreshold, mean = accR[0], std = accR[1], nSamples = nSamples[1] });

        controller.SamplingCallbacks.Remove("fixation");
    }

    public delegate void validationCallbackDel(bool ans);
    public validationCallbackDel validationCallback = ans => { };

    IEnumerator ValidationProcedure()
    {
        bool fixationDone;
        bool[] fixationAcc = { false, false };
        fixationCallback = (res) =>
        {
            fixationDone = true;
            fixationAcc[res.eye == "left" ? 0 : 1] = res.mean < accThreshold;
            print(
                $"eye {res.eye}: mean {res.mean}, std {res.std}, success {res.success}, nSamples {res.nSamples}");
        };
        int successes = 0;

        ringValidationStack = getRingsList(9);

        for (int i = 0; i < 9; i++)
        {
            int ringIdx = ringValidationStack.Pop();
            Vector2 ringPos = Utils.getTargetPosFromIdx(ringIdx, 3, 3, new Vector2(.4f, .6f), new Vector2(.4f, .6f), aspectRatio);

            ringPos.x *= Utils.Cam_FOV_hori;
            ringPos.y *= Utils.Cam_FOV_vert;

            print($"pts {i}, idx {ringIdx}, pos {ringPos.x}, {ringPos.y}");

            setValidationRingIdx(ringIdx);

            fixationAcc = new[] { false, false }; fixationDone = false;
            StartCoroutine(nameof(FixationProcedure), ringPos);
            yield return new WaitUntil(() => fixationDone);

            successes += (fixationAcc.Sum(a => a ? 1 : 0) > 0) ? 1 : 0;

            //			if (successes != (i+1)){
            //				print($"Stop validation early -- after ring #{i}");
            //				break;
            //			}
        }
        validationCallback(successes >= 7);
    }

    private void setValidationRingIdx(int idx)
    {
        _renderer.material.SetInt(TargetCircle, idx);
    }
}
