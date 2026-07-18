using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Newtonsoft.Json;
using System.Diagnostics;

[Serializable]
public class LoginResultData
{
    public string status;
    public PlayerSaveData playerSaveData;
}

public class LoginManager : SerializedMonoBehaviour
{

    [FoldoutGroup("Web Connection")]
    [SerializeField] EventTrackerTrigger eventTrackerTrigger;

    public string runResult;
    public string warningMessage;
    
    public void SignUpFunction(string username, string password)
    {
        runResult = "";
        warningMessage = "";
        Dictionary<string, string> form = new();
        form.Add("username", username);

        string encoderPassword = PasswordEncoder.GetMd5Hash(password);
        form.Add("password", encoderPassword);

        StartCoroutine(SignUpRequest(form, (result) => {
            if (result.Contains("successful"))
            {
                LoginFunction(username, password);
            }
            else if (result.Contains("already sign up"))
            {
                runResult = "failed";
                warningMessage = "already sign up";
            }
            else if (result.Contains("Cannot connect to destination host"))
            {
                runResult = "failed";
                warningMessage = "Cannot connect";
            }
        }));
    }

    IEnumerator SignUpRequest(Dictionary<string, string> form, Action<string> callback)
    {
        UnityWebRequest www = BuildPostRequest($"{UrlSetting.Instance.GetUrl()}signUp", form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callback(www.error);
        }
        else
        {
            callback(www.downloadHandler.text);
        }
        www.Dispose();
    }

    public void LoginFunction(string username, string password)
    {
        runResult = "";
        warningMessage = "";

        Dictionary<string, string> form = new();
        form.Add("username", username);

        string encoderPassword = PasswordEncoder.GetMd5Hash(password);
        form.Add("password", encoderPassword);

        StartCoroutine(LoginRequest(form, (result) => {

            if (result.Contains("successful"))
            {
                LoginResultData loginResultData = JsonUtility.FromJson<LoginResultData>(result);
                SaveManager.Instance.LoadPlayerSaveData(username, loginResultData.playerSaveData);
                eventTrackerTrigger.SendEvent("Login", "Success");
                runResult = "successful";
            }
            else if (result.Contains("username not found"))
            {
                runResult = "failed";
                warningMessage = "username not found";
            }
            else if (result.Contains("password incorrect")){
                runResult = "failed";
                warningMessage = "password incorrect";
            }else if (result.Contains("Cannot connect to destination host"))
            {
                runResult = "failed";
                warningMessage = "Cannot connect";
            }
        }));


    }

    IEnumerator LoginRequest(Dictionary<string, string> form, Action<string> callback)
    {


        UnityWebRequest www = BuildPostRequest($"{UrlSetting.Instance.GetUrl()}login", form);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callback(www.error);
        }
        else
        {
            callback(www.downloadHandler.text);
        }
        www.Dispose();
    }

    UnityWebRequest BuildPostRequest(string url, Dictionary<string, string> form)
    {
        List<string> fields = new();
        foreach (var field in form)
        {
            fields.Add($"{UnityWebRequest.EscapeURL(field.Key)}={UnityWebRequest.EscapeURL(field.Value)}");
        }

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(string.Join("&", fields));
        UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=utf-8");
        return request;
    }
}

