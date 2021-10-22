﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Scada.Comm.Drivers.DrvModbus.Protocol
{
    /// <summary>
    /// Represents a Modbus command.
    /// <para>Представляет команду Modbus.</para>
    /// </summary>
    public class ModbusCmd : DataUnit
    {
        private string reqDescr; // the command description


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public ModbusCmd(DataBlock dataBlock, bool multiple)
            : base(dataBlock)
        {
            if (!(dataBlock == DataBlock.Coils || dataBlock == DataBlock.HoldingRegisters))
                throw new InvalidOperationException(ModbusPhrases.IllegalDataTable);

            reqDescr = "";
            Multiple = multiple;
            ElemType = ElemType.Undefined;
            ElemCnt = 1;
            ByteOrder = null;
            CmdNum = 1;
            Value = 0;
            Data = null;

            // define function codes
            UpdateFuncCode();
            ExcFuncCode = (byte)(FuncCode | 0x80);
        }


        /// <summary>
        /// Gets a description of the request that writes the command.
        /// </summary>
        public override string ReqDescr
        {
            get
            {
                if (string.IsNullOrEmpty(reqDescr))
                    reqDescr = string.Format(ModbusPhrases.Command, Name);
                return reqDescr;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the command writes multiple elements.
        /// </summary>
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the command element type.
        /// </summary>
        public ElemType ElemType { get; set; }

        /// <summary>
        /// Gets or sets the number of elements written by the command.
        /// </summary>
        public int ElemCnt { get; set; }

        /// <summary>
        /// Gets or sets the byte order.
        /// </summary>
        public int[] ByteOrder { get; set; }

        /// <summary>
        /// Gets or sets the device command number.
        /// </summary>
        public int CmdNum { get; set; }

        /// <summary>
        /// Gets or sets the command value.
        /// </summary>
        public ushort Value { get; set; }

        /// <summary>
        /// Gets or sets the data of the multiple command.
        /// </summary>
        public byte[] Data { get; set; }


        /// <summary>
        /// Initializes the request PDU and calculates the response length.
        /// </summary>
        public override void InitReqPDU()
        {
            if (Multiple)
            {
                // build PDU for WriteMultipleCoils and WriteMultipleRegisters commands
                int quantity;   // quantity of registers
                int dataLength; // data length in bytes

                if (DataBlock == DataBlock.Coils)
                {
                    quantity = ElemCnt;
                    dataLength = (ElemCnt % 8 == 0) ? ElemCnt / 8 : ElemCnt / 8 + 1;
                }
                else
                {
                    quantity = ElemCnt * ModbusUtils.GetQuantity(ElemType);
                    dataLength = quantity * 2;
                }

                ReqPDU = new byte[6 + dataLength];
                ReqPDU[0] = FuncCode;
                ReqPDU[1] = (byte)(Address / 256);
                ReqPDU[2] = (byte)(Address % 256);
                ReqPDU[3] = (byte)(quantity / 256);
                ReqPDU[4] = (byte)(quantity % 256);
                ReqPDU[5] = (byte)dataLength;

                ModbusUtils.ApplyByteOrder(Data, 0, ReqPDU, 6, dataLength, ByteOrder, false);

                // set response length
                RespPduLen = 5;
            }
            else
            {
                // build PDU for WriteSingleCoil and WriteSingleRegister commands
                int dataLength = DataBlock == DataBlock.Coils ? 2 : ModbusUtils.GetDataLength(ElemType);
                ReqPDU = new byte[3 + dataLength];
                ReqPDU[0] = FuncCode;
                ReqPDU[1] = (byte)(Address / 256);
                ReqPDU[2] = (byte)(Address % 256);

                if (DataBlock == DataBlock.Coils)
                {
                    ReqPDU[3] = Value > 0 ? (byte)0xFF : (byte)0x00;
                    ReqPDU[4] = 0x00;
                }
                else
                {

                    byte[] data = dataLength == 2 ?
                        new byte[] // standard Modbus
                        {
                            (byte)(Value / 256),
                            (byte)(Value % 256)
                        } :
                        Data;

                    ModbusUtils.ApplyByteOrder(data, 0, ReqPDU, 3, dataLength, ByteOrder, false);
                }

                // set response length
                RespPduLen = ReqPDU.Length; // echo
            }
        }

        /// <summary>
        /// Decodes the response PDU.
        /// </summary>
        public override bool DecodeRespPDU(byte[] buffer, int offset, int length, out string errMsg)
        {
            if (base.DecodeRespPDU(buffer, offset, length, out errMsg))
            {
                if (buffer[offset + 1] == ReqPDU[1] && buffer[offset + 2] == ReqPDU[2] &&
                    buffer[offset + 3] == ReqPDU[3] && buffer[offset + 4] == ReqPDU[4])
                {
                    return true;
                }
                else
                {
                    errMsg = ModbusPhrases.IncorrectPduData;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the function code according to the data block.
        /// </summary>
        public void UpdateFuncCode()
        {
            if (DataBlock == DataBlock.Coils)
                FuncCode = Multiple ? FuncCodes.WriteMultipleCoils : FuncCodes.WriteSingleCoil;
            else if (DataBlock == DataBlock.HoldingRegisters)
                FuncCode = Multiple ? FuncCodes.WriteMultipleRegisters : FuncCodes.WriteSingleRegister;
        }

        /// <summary>
        /// Sets the command data, converted according to the command element type.
        /// </summary>
        public void SetCmdData(double cmdVal)
        {
            bool reverse = true;

            switch (ElemType)
            {
                case ElemType.UShort:
                    Data = BitConverter.GetBytes((ushort)cmdVal);
                    break;
                case ElemType.Short:
                    Data = BitConverter.GetBytes((short)cmdVal);
                    break;
                case ElemType.UInt:
                    Data = BitConverter.GetBytes((uint)cmdVal);
                    break;
                case ElemType.Int:
                    Data = BitConverter.GetBytes((int)cmdVal);
                    break;
                case ElemType.ULong:
                    Data = BitConverter.GetBytes((ulong)cmdVal);
                    break;
                case ElemType.Long:
                    Data = BitConverter.GetBytes((long)cmdVal);
                    break;
                case ElemType.Float:
                    Data = BitConverter.GetBytes((float)cmdVal);
                    break;
                case ElemType.Double:
                    Data = BitConverter.GetBytes(cmdVal);
                    break;
                default:
                    Data = BitConverter.GetBytes(cmdVal);
                    reverse = false;
                    break;
            }

            if (reverse)
                Array.Reverse(Data);
        }
    }
}
