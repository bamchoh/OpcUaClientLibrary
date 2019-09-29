# -*- coding: utf-8 -*-

import time
import clr
import sys

clr.AddReference("System.Collections")
clr.AddReference(".\lib\OpcUa")

from System.Collections.Generic import Dictionary
from System import *
from OpcUa import OpcUa

def main():
    var01 = "ns=2;s=Scalar_Static_Boolean"
    var02 = "ns=2;s=Scalar_Static_SByte"
    var03 = "ns=2;s=Scalar_Static_Int16"
    var04 = "ns=2;s=Scalar_Static_Int32"
    var05 = "ns=2;s=Scalar_Static_Int64"
    var06 = "ns=2;s=Scalar_Static_Byte"
    var07 = "ns=2;s=Scalar_Static_UInt16"
    var08 = "ns=2;s=Scalar_Static_UInt32"
    var09 = "ns=2;s=Scalar_Static_UInt64"
    var10 = "ns=2;s=Scalar_Static_Double"
    var11 = "ns=2;s=Scalar_Static_Float"
    var12 = "ns=2;s=Scalar_Static_String"

    opcua = OpcUa()
    opcua.ConfigSectionName = "Opc.Ua.Client.Python"
    opcua.Open("opc.tcp://localhost:62541/Quickstarts/ReferenceServer")
    # security connection with user/pass
    # opcua.Open("opc.tcp://localhost:62541/Quickstarts/ReferenceServer", True, "user", "pass");

    dict1 = Dictionary[String, Object]()
    dict1[var01] = True
    dict1[var02] = SByte(123)
    dict1[var03] = Int16(12345)
    dict1[var04] = Int32(1234567890)
    dict1[var05] = Int64(12345678901234)
    dict1[var06] = Byte(123)
    dict1[var07] = UInt16(12345)
    dict1[var08] = UInt32(1234567890)
    dict1[var09] = UInt64(12345678901234)
    dict1[var10] = Double(1.23456789)
    dict1[var11] = Single(1.234)
    dict1[var12] = "abcdefghijklmnopqrstuvwxyz"

    dict2 = Dictionary[String, Object]()
    dict2[var01] = False
    dict2[var02] = SByte(-123)
    dict2[var03] = Int16(-12345)
    dict2[var04] = Int32(-1234567890)
    dict2[var05] = Int64(-12345678901234)
    dict2[var06] = Byte(254)
    dict2[var07] = UInt16(65534)
    dict2[var08] = UInt32(4294967294)
    dict2[var09] = UInt64(12345678901234)
    dict2[var10] = Double(-1.23456789)
    dict2[var11] = Single(-1.234)
    dict2[var12] = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"


    py_array = [var01, var02, var03, var04, var05, var06, var07, var08, var09, var10, var11, var12]

    # write to standard
    # f = sys.stdout
    
    # write to file
    with open("result.txt", mode='w') as f:
        for i in range(3):
            values = opcua.Read(Array[String](py_array));

            for key in py_array:
                print(
                        key,
                        values[key].StatusCode,
                        values[key].WrappedValue.TypeInfo,
                        values[key].ServerTimestamp,
                        values[key].Value, file=f)
            print(file=f)

            if i % 2 == 0:
                opcua.Write(dict1)
            else:
                opcua.Write(dict2)

            time.sleep(1)

    opcua.Close()

if __name__ == '__main__':
    main()
