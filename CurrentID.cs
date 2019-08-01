using System;

public static class CurrentID
{

    static string __CurrentPatronID;
    static string __CurrentItemID;

    public static string CurrentPatronID
    {
        get
        {
            return __CurrentPatronID;
        }
        set
        {
            __CurrentPatronID = value;
        }
    }

    public static string CurrentItemID
    {
        get
        {
            return __CurrentItemID;
        }
        set
        {
            __CurrentItemID = value;
        }
    }

}
