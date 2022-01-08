﻿using Mesen.Interop;
using Mesen.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Globalization;
using System.Text;

namespace Mesen.Debugger.Labels
{
	public class CodeLabel : ReactiveObject
	{
		[Reactive] public UInt32 Address { get; set; }
		[Reactive] public SnesMemoryType MemoryType { get; set; }
		[Reactive] public string Label { get; set; } = "";
		[Reactive] public string Comment { get; set; } = "";
		[Reactive] public CodeLabelFlags Flags { get; set; }
		[Reactive] public UInt32 Length { get; set; } = 1;

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			switch(MemoryType) {
				case SnesMemoryType.Register: sb.Append("REG:"); break;
				case SnesMemoryType.PrgRom: sb.Append("PRG:"); break;
				case SnesMemoryType.WorkRam: sb.Append("WORK:"); break;
				case SnesMemoryType.SaveRam: sb.Append("SAVE:"); break;
				case SnesMemoryType.Sa1InternalRam: sb.Append("IRAM:"); break;
				case SnesMemoryType.SpcRam: sb.Append("SPCRAM:"); break;
				case SnesMemoryType.SpcRom: sb.Append("SPCROM:"); break;
				case SnesMemoryType.BsxPsRam: sb.Append("PSRAM:"); break;
				case SnesMemoryType.BsxMemoryPack: sb.Append("MPACK:"); break;
				case SnesMemoryType.DspProgramRom: sb.Append("DSPPRG:"); break;
				case SnesMemoryType.GbPrgRom: sb.Append("GBPRG:"); break;
				case SnesMemoryType.GbWorkRam: sb.Append("GBWRAM:"); break;
				case SnesMemoryType.GbCartRam: sb.Append("GBSRAM:"); break;
				case SnesMemoryType.GbHighRam: sb.Append("GBHRAM:"); break;
				case SnesMemoryType.GbBootRom: sb.Append("GBBOOT:"); break;
				case SnesMemoryType.GameboyMemory: sb.Append("GBREG:"); break;
			}

			sb.Append(Address.ToString("X4"));
			if(Length > 1) {
				sb.Append("-" + (Address+Length-1).ToString("X4"));
			}
			sb.Append(":");
			sb.Append(Label);
			if(!string.IsNullOrWhiteSpace(Comment)) {
				sb.Append(":");
				sb.Append(Comment.Replace(Environment.NewLine, "\\n").Replace("\n", "\\n").Replace("\r", "\\n"));
			}
			return sb.ToString();
		}

		private static char[] _separator = new char[1] { ':' };
		public static CodeLabel? FromString(string data)
		{
			string[] rowData = data.Split(_separator, 4);
			if(rowData.Length < 3) {
				//Invalid row
				return null;
			}

			SnesMemoryType type;
			switch(rowData[0]) {
				case "REG": type = SnesMemoryType.Register; break;
				case "PRG": type = SnesMemoryType.PrgRom; break;
				case "SAVE": type = SnesMemoryType.SaveRam; break;
				case "WORK": type = SnesMemoryType.WorkRam; break;
				case "IRAM": type = SnesMemoryType.Sa1InternalRam; break;
				case "SPCRAM": type = SnesMemoryType.SpcRam; break;
				case "SPCROM": type = SnesMemoryType.SpcRom; break;
				case "PSRAM": type = SnesMemoryType.BsxPsRam; break;
				case "MPACK": type = SnesMemoryType.BsxMemoryPack; break;
				case "DSPPRG": type = SnesMemoryType.DspProgramRom; break;
				case "GBPRG": type = SnesMemoryType.GbPrgRom; break;
				case "GBWRAM": type = SnesMemoryType.GbWorkRam; break;
				case "GBSRAM": type = SnesMemoryType.GbCartRam; break;
				case "GBHRAM": type = SnesMemoryType.GbHighRam; break;
				case "GBBOOT": type = SnesMemoryType.GbBootRom; break;
				case "GBREG": type = SnesMemoryType.GameboyMemory; break;
				default: return null;
			}

			string addressString = rowData[1];
			uint address = 0;
			uint length = 1;
			if(addressString.Contains("-")) {
				uint addressEnd;
				string[] addressStartEnd = addressString.Split('-');
				if(UInt32.TryParse(addressStartEnd[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address) &&
					UInt32.TryParse(addressStartEnd[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addressEnd)) {
					if(addressEnd > address) {
						length = addressEnd - address + 1;
					} else {
						//Invalid label (start < end)
						return null;
					}
				} else {
					//Invalid label (can't parse)
					return null;
				}
			} else {
				if(!UInt32.TryParse(rowData[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address)) {
					//Invalid label (can't parse)
					return null;
				}
				length = 1;
			}

			string labelName = rowData[2];
			if(!string.IsNullOrEmpty(labelName) && !LabelManager.LabelRegex.IsMatch(labelName)) {
				//Reject labels that don't respect the label naming restrictions
				return null;
			}

			CodeLabel codeLabel;
			codeLabel = new CodeLabel {
				Address = address,
				MemoryType = type,
				Label = labelName,
				Length = length,
				Comment = ""
			};

			if(rowData.Length > 3) {
				codeLabel.Comment = rowData[3].Replace("\\n", "\n");
			}

			return codeLabel;
		}

		public bool Matches(CpuType type)
		{
			return type == this.MemoryType.ToCpuType();
		}

		public AddressInfo GetAbsoluteAddress()
		{
			return new AddressInfo() { Address = (int)this.Address, Type = this.MemoryType };
		}

		public AddressInfo GetRelativeAddress(CpuType cpuType)
		{
			if(MemoryType.IsRelativeMemory()) {
				return GetAbsoluteAddress();
			}
			return DebugApi.GetRelativeAddress(GetAbsoluteAddress(), cpuType);
		}

		public byte GetValue()
		{
			return DebugApi.GetMemoryValue(this.MemoryType, this.Address);
		}

		public CodeLabel Clone()
		{
			return JsonHelper.Clone(this);
		}

		public void CopyFrom(CodeLabel copy)
		{
			Address = copy.Address;
			MemoryType = copy.MemoryType;
			Label = copy.Label;
			Comment = copy.Comment;
			Flags = copy.Flags;
			Length = copy.Length;
		}
	}
}
