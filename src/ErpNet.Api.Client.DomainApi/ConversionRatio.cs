using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ErpNet.Api.Client
{
    /// <summary>
    /// Represents a conversion ratio used to convert currency amounts or quantities to different units.
    /// </summary>
    public struct ConversionRatio
    {
        /// <summary>
        /// Creates an instance of <see cref="ConversionRatio"/>
        /// </summary>
        /// <param name="m"></param>
        /// <param name="d"></param>
        public ConversionRatio(decimal m, decimal d)
        {
            this.Multiplier = m;
            this.Divisor = d;
        }

        /// <summary>
        /// The ratio multiplier
        /// </summary>
        public decimal Multiplier { get; }
        /// <summary>
        /// The ration divisor
        /// </summary>
        public decimal Divisor { get; }

        /// <summary>
        /// A zero <see cref="ConversionRatio"/>
        /// </summary>
        public static readonly ConversionRatio Zero = new ConversionRatio(0, 1);

        ///<inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}/{1}", Multiplier, Divisor);
        }

        /// <summary>
        /// Gets the conversion ratio from the source to the destination unit. The ratio is calculated through the base unit.
        /// If the ratio is not available, <see cref="ConversionRatio.Zero"/> is returned.
        /// </summary>
        /// <typeparam name="TUnit"></typeparam>
        /// <param name="getRatioToBaseOrDefault"></param>
        /// <param name="sourceUnit"></param>
        /// <param name="destinationUnit"></param>
        /// <returns></returns>
        public static async Task<ConversionRatio> GetRatioThroughBaseOrDefault<TUnit>(
            Func<TUnit, Task<ConversionRatio>> getRatioToBaseOrDefault,
            TUnit sourceUnit,
            TUnit destinationUnit)
        {
            if (sourceUnit == null && destinationUnit == null)
                return new ConversionRatio(1, 1);
            if (sourceUnit == null || destinationUnit == null)
                return ConversionRatio.Zero;
            if (sourceUnit.Equals(destinationUnit))
                return new ConversionRatio(1, 1);

            ConversionRatio sourceToBase = await getRatioToBaseOrDefault(sourceUnit);
            ConversionRatio destinationToBase = await getRatioToBaseOrDefault(destinationUnit);

            if (sourceToBase == ConversionRatio.Zero || destinationToBase == ConversionRatio.Zero)
                return ConversionRatio.Zero;

            return ConversionRatio.CombineThroughBase(sourceToBase, destinationToBase);
        }

		/// <summary>
		/// Combines two <see cref="ConversionRatio"/> instances, which are conversion ratios against a single base unit of measure.
		/// </summary>
		/// <param name="sourceToBase">The <see cref="ConversionRatio"/> from the source unit of measure to the base unit of measure.</param>
		/// <param name="destinationToBase">The <see cref="ConversionRatio"/> from the destination unit of measure to the base unit of measure.</param>
		/// <returns>The combined <see cref="ConversionRatio"/>.</returns>
		/// <remarks>
		/// <para>
		/// <see cref="CombineThroughBase"/> combines two <see cref="ConversionRatio"/> instances in a single instance. 
		/// The two instances should represent respectively the conversion ratio from the source 
		/// and the destination unit of measure to the base unit of measure. 
		/// </para>
		/// <para>
		/// The method is usefull to combine two conversions in one, without loosing precision.
		/// </para>
		/// </remarks>
		/// <example>
		/// //pieces is used as the base measurement unit
		/// ConversionRatio boxesToPieces = new ConversionRatio(12, 1); //12 pieces in a box
		/// ConversionRatio kilogramsToPieces = new ConversionRatio(1, 2); //Each piece is 2 kg.
		/// 
		/// //combine the two conversion ratios, using the pieces as the base unit of measure
		/// ConversionRatio boxesToKilograms = ConversionRatio.CombineThroughBase(boxesToPieces, kilogramsToPieces);
		/// </example>
		public static ConversionRatio CombineThroughBase(ConversionRatio sourceToBase, ConversionRatio destinationToBase)
        {
            return new ConversionRatio(sourceToBase.Multiplier * destinationToBase.Divisor, sourceToBase.Divisor * destinationToBase.Multiplier);
        }

		/// <summary>
		/// Implements the operator ==.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator ==(ConversionRatio one, ConversionRatio other)
		{
			return one.Equals(other);
		}

		/// <summary>
		/// Implements the operator !=.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static bool operator !=(ConversionRatio one, ConversionRatio other)
		{
			return !one.Equals(other);
		}

		/// <summary>
		/// Implements the operator *. This multiplies the multipliers and the divisors.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static ConversionRatio operator *(ConversionRatio one, ConversionRatio other)
		{
			return Multiply(one, other);
		}

		/// <summary>
		/// Multiplies the specified ratios multipliers and the divisors.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public static ConversionRatio Multiply(ConversionRatio one, ConversionRatio other)
		{
			return new ConversionRatio(one.Multiplier * other.Multiplier, one.Divisor * other.Divisor);
		}

		/// <summary>
		/// Implements the operator *. This multiplies the multipliers and the divisors.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static ConversionRatio operator *(ConversionRatio one, decimal other)
		{
			return Multiply(one, other);
		}

		/// <summary>
		/// Multiplies the specified ratios multipliers and the divisors.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		public static ConversionRatio Multiply(ConversionRatio one, decimal other)
		{
			return new ConversionRatio(one.Multiplier * other, one.Divisor);
		}

		/// <summary>
		/// Implements the operator *. This multiplies the multipliers and the divisors.
		/// </summary>
		/// <param name="one">The one.</param>
		/// <param name="other">The other.</param>
		/// <returns>
		/// The result of the operator.
		/// </returns>
		public static ConversionRatio operator /(ConversionRatio one, decimal other)
		{
			return Divide(one, other);
		}

		/// <summary>
		/// Divides the specified ratio by the specified divisor.
		/// </summary>
		/// <param name="ratio">The ratio to divide.</param>
		/// <param name="divisor">The divisor.</param>
		/// <returns></returns>
		public static ConversionRatio Divide(ConversionRatio ratio, decimal divisor)
		{
			return new ConversionRatio(ratio.Multiplier, ratio.Divisor * divisor);
		}

		/// <summary>
		/// Determines whether the specified <see cref="global::System.Object" />, is equal to this instance.
		/// </summary>
		/// <param name="obj">The <see cref="global::System.Object" /> to compare with this instance.</param>
		/// <returns>
		///   <c>true</c> if the specified <see cref="global::System.Object" /> is equal to this instance; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is ConversionRatio))
				return false;

			return this.Equals((ConversionRatio)obj);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		/// <returns>
		/// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
		/// </returns>
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + Multiplier.GetHashCode();
			hash = hash * 23 + Divisor.GetHashCode();
			return hash;
		}

	}

}
