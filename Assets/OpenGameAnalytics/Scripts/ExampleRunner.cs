using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OGA;

public class ExampleRunner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TestSaveData();
    }

    private void TestSaveData() {
        // Saves this user's age as 32
        OpenGameAnalytics.instance.SaveUserInfo("age", "32");

        // Saves that this user's study ID is 00012
        OpenGameAnalytics.instance.SaveUserInfo("study_id", "00012");

        // Saves that the user choose option #4 during choice #2
        OpenGameAnalytics.instance.SaveUserAction("choice2", "4");

        // Saves that the user was randomly assigned to the 1st person condition
        OpenGameAnalytics.instance.AssignCondition("perspective", "1st-person");

        // Saves that the user chose answer 'A' for the first question of some quiz
        OpenGameAnalytics.instance.SaveStudyEndpoint("QuizQuestion1", "A");
        // ^^^ Would only use this if there's something in the game itself to measure an endpoint
        // i.e. something we may expect to be changed by the game
    }
}
