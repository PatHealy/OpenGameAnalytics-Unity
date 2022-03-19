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
        // Whenever we want to assign a study id to this user, to connect game data to other data, do this:
        OpenGameAnalytics.instance.SaveUserInfo("study_id", "00012");

        // We save demographic information about the user in a similar way:
        OpenGameAnalytics.instance.SaveUserInfo("age", "32");

        // Whenever the game assigns experimental conditions, tell the server like this:
        OpenGameAnalytics.instance.AssignCondition("perspective", "1st-person");

        // Whenever the player does some action we want to keep track of (like making a particular choice), do this:
        OpenGameAnalytics.instance.SaveUserAction("choice2", "4"); //i.e. on choice2, chose option 4

        // Imagine we used the game to measure some learning outcome through a quiz
        // We could save the player's answer to quiz question 1 like this:
        OpenGameAnalytics.instance.SaveStudyEndpoint("QuizQuestion1", "A");
    }



}
